using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class RagdollCreator : EditorWindow
{
    private GameObject selectedCharacter;

    [MenuItem("Tools/Ragdoll Creator")]
    public static void ShowWindow()
    {
        RagdollCreator window = GetWindow<RagdollCreator>("Ragdoll Creator");
        window.minSize = new Vector2(300, 150);
    }

    private void OnGUI()
    {
        GUILayout.Label("Mixamo Ragdoll Generator (Unity 6 Ready)", EditorStyles.boldLabel);
        selectedCharacter = (GameObject)EditorGUILayout.ObjectField("Character Root", selectedCharacter, typeof(GameObject), true);

        if (GUILayout.Button("Generate Ragdoll") && selectedCharacter != null)
        {
            GenerateRagdoll(selectedCharacter);
        }
    }

    private static void GenerateRagdoll(GameObject root)
    {
        if (root == null) return;

        Undo.RegisterFullObjectHierarchyUndo(root, "Generate Ragdoll");

        Animator animator = root.GetComponentInChildren<Animator>();
        if (animator == null || !animator.isHuman || animator.avatar == null)
        {
            Debug.LogError("Selected object must have a valid humanoid Animator with an Avatar.");
            return;
        }

        Dictionary<string, HumanBodyBones> boneMap = new()
        {
            { "Hips", HumanBodyBones.Hips },
            { "Spine", HumanBodyBones.Spine },
            { "Chest", HumanBodyBones.Chest },
            { "Head", HumanBodyBones.Head },
            { "LeftUpperArm", HumanBodyBones.LeftUpperArm },
            { "LeftLowerArm", HumanBodyBones.LeftLowerArm },
            { "RightUpperArm", HumanBodyBones.RightUpperArm },
            { "RightLowerArm", HumanBodyBones.RightLowerArm },
            { "LeftUpperLeg", HumanBodyBones.LeftUpperLeg },
            { "LeftLowerLeg", HumanBodyBones.LeftLowerLeg },
            { "RightUpperLeg", HumanBodyBones.RightUpperLeg },
            { "RightLowerLeg", HumanBodyBones.RightLowerLeg },
        };

        Dictionary<string, Transform> bones = new();
        foreach (var kvp in boneMap)
        {
            var t = animator.GetBoneTransform(kvp.Value);
            if (t != null)
                bones[kvp.Key] = t;
            else
                Debug.LogWarning($"Missing bone: {kvp.Key}");
        }

        if (!bones.ContainsKey("Hips"))
        {
            Debug.LogError("🦴 'Hips' bone not found — ragdoll cannot be generated.");
            return;
        }

        Dictionary<string, float> massMap = new()
        {
            { "Hips", 10f },
            { "Spine", 8f },
            { "Chest", 8f },
            { "Head", 5f },
            { "LeftUpperArm", 2.5f },
            { "LeftLowerArm", 1.5f },
            { "RightUpperArm", 2.5f },
            { "RightLowerArm", 1.5f },
            { "LeftUpperLeg", 7f },
            { "LeftLowerLeg", 5f },
            { "RightUpperLeg", 7f },
            { "RightLowerLeg", 5f },
        };

        foreach (var (name, bone) in bones)
        {
            if (bone == null) continue;

            // Set tag to Enemy for all ragdoll bones
            bone.gameObject.tag = "Enemy";
            Debug.Log($"Tagged {bone.name} as Enemy");

            Rigidbody rb = bone.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = bone.gameObject.AddComponent<Rigidbody>();
            }

            if (rb == null)
            {
                Debug.LogError($"❌ Failed to add Rigidbody to {bone.name}");
                continue;
            }

            rb.mass = massMap.TryGetValue(name, out float mass) ? mass : 1f;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.05f;

            if (!bone.GetComponent<Collider>())
            {
                CapsuleCollider col = bone.gameObject.AddComponent<CapsuleCollider>();
                FitCapsuleCollider(col, bone, bones, name);
            }
        }

        CreateJoint(bones, "LeftLowerArm", "LeftUpperArm");
        CreateJoint(bones, "RightLowerArm", "RightUpperArm");
        CreateJoint(bones, "LeftUpperArm", "Chest");
        CreateJoint(bones, "RightUpperArm", "Chest");
        CreateJoint(bones, "Head", "Chest");
        CreateJoint(bones, "Chest", "Spine");
        CreateJoint(bones, "Spine", "Hips");
        CreateJoint(bones, "LeftUpperLeg", "Hips");
        CreateJoint(bones, "RightUpperLeg", "Hips");
        CreateJoint(bones, "LeftLowerLeg", "LeftUpperLeg");
        CreateJoint(bones, "RightLowerLeg", "RightUpperLeg");

        // Also tag the root object as Enemy if it isn't already
        if (root.tag != "Enemy")
        {
            root.tag = "Enemy";
            Debug.Log($"Tagged root object {root.name} as Enemy");
        }

        Debug.Log("✅ Ragdoll created successfully with custom mass, capsule fitting, and Enemy tags applied to all bones.");
    }

   private static void CreateJoint(Dictionary<string, Transform> bones, string childName, string parentName)
{
    if (!bones.ContainsKey(childName) || !bones.ContainsKey(parentName))
        return;

    Transform child = bones[childName];
    Transform parent = bones[parentName];

    CharacterJoint joint = child.GetComponent<CharacterJoint>();
    if (joint == null)
    {
        joint = child.gameObject.AddComponent<CharacterJoint>();
    }

    if (joint == null)
    {
        Debug.LogError($"❌ Failed to add CharacterJoint to {child.name}");
        return;
    }

    Rigidbody parentRb = parent.GetComponent<Rigidbody>();
    if (parentRb == null)
    {
        Debug.LogWarning($"⚠️ Parent bone '{parent.name}' is missing a Rigidbody.");
        return;
    }

    joint.connectedBody = parentRb;
    joint.enablePreprocessing = false;

    joint.axis = Vector3.right;
    joint.swingAxis = Vector3.up;

    joint.lowTwistLimit = new SoftJointLimit { limit = -40 };
    joint.highTwistLimit = new SoftJointLimit { limit = 40 };
    joint.swing1Limit = new SoftJointLimit { limit = 30 };
    joint.swing2Limit = new SoftJointLimit { limit = 30 };
}

    private static void FitCapsuleCollider(CapsuleCollider col, Transform bone, Dictionary<string, Transform> bones, string boneName)
    {
        // Define specific sizes for each bone type
        Dictionary<string, Vector3> boneSizes = new()
        {
            { "Hips", new Vector3(0.25f, 0.15f, 0.20f) },
            { "Spine", new Vector3(0.20f, 0.25f, 0.15f) },
            { "Chest", new Vector3(0.30f, 0.25f, 0.20f) },
            { "Head", new Vector3(0.18f, 0.22f, 0.18f) },
            { "LeftUpperArm", new Vector3(0.08f, 0.25f, 0.08f) },
            { "RightUpperArm", new Vector3(0.08f, 0.25f, 0.08f) },
            { "LeftLowerArm", new Vector3(0.06f, 0.22f, 0.06f) },
            { "RightLowerArm", new Vector3(0.06f, 0.22f, 0.06f) },
            { "LeftUpperLeg", new Vector3(0.12f, 0.35f, 0.12f) },
            { "RightUpperLeg", new Vector3(0.12f, 0.35f, 0.12f) },
            { "LeftLowerLeg", new Vector3(0.08f, 0.32f, 0.08f) },
            { "RightLowerLeg", new Vector3(0.08f, 0.32f, 0.08f) }
        };

        // Try to find child bone for automatic sizing
        Transform child = null;
        
        // Look for specific child bones based on bone name
        if (boneName.Contains("Upper"))
        {
            string lowerName = boneName.Replace("Upper", "Lower");
            if (bones.ContainsKey(lowerName))
                child = bones[lowerName];
        }
        else if (boneName == "Chest" && bones.ContainsKey("Head"))
        {
            child = bones["Head"];
        }
        else if (boneName == "Spine" && bones.ContainsKey("Chest"))
        {
            child = bones["Chest"];
        }
        
        // If no direct child found, look through actual children
        if (child == null)
        {
            foreach (Transform t in bone)
            {
                if (t.name.ToLower().Contains("lower") || 
                    t.name.ToLower().Contains("hand") || 
                    t.name.ToLower().Contains("foot") ||
                    t.name.ToLower().Contains("head"))
                {
                    child = t;
                    break;
                }
            }
        }

        Vector3 size;
        if (boneSizes.ContainsKey(boneName))
        {
            size = boneSizes[boneName];
        }
        else
        {
            // Fallback default size
            size = new Vector3(0.1f, 0.2f, 0.1f);
        }

        // Calculate length and direction based on child bone if available
        float length = size.y; // Default height
        int direction = 1; // Y-axis default
        
        if (child != null)
        {
            Vector3 localDir = bone.InverseTransformDirection(child.position - bone.position);
            
            // Find the axis with the largest component
            if (Mathf.Abs(localDir.x) > Mathf.Abs(localDir.y) && Mathf.Abs(localDir.x) > Mathf.Abs(localDir.z))
            {
                direction = 0; // X-axis
                length = Mathf.Max(Vector3.Distance(bone.position, child.position), size.x);
            }
            else if (Mathf.Abs(localDir.z) > Mathf.Abs(localDir.y) && Mathf.Abs(localDir.z) > Mathf.Abs(localDir.x))
            {
                direction = 2; // Z-axis
                length = Mathf.Max(Vector3.Distance(bone.position, child.position), size.z);
            }
            else
            {
                direction = 1; // Y-axis
                length = Mathf.Max(Vector3.Distance(bone.position, child.position), size.y);
            }
        }

        // Apply the collider settings
        col.direction = direction;
        col.height = length;
        
        // Calculate radius based on the other two dimensions
        float radius;
        switch (direction)
        {
            case 0: // X-axis
                radius = Mathf.Max(size.y, size.z) * 0.5f;
                break;
            case 2: // Z-axis
                radius = Mathf.Max(size.x, size.y) * 0.5f;
                break;
            default: // Y-axis
                radius = Mathf.Max(size.x, size.z) * 0.5f;
                break;
        }
        
        col.radius = radius;
        
        // Set center offset - collider should start at bone origin and extend toward child
        Vector3 center = Vector3.zero;
        
        if (child != null)
        {
            // Calculate the direction from bone to child in local space
            Vector3 localChildDir = bone.InverseTransformPoint(child.position);
            
            // Center the collider so it starts at bone origin and extends to child
            center = localChildDir * 0.5f;
        }
        else
        {
            // Fallback for bones without clear children
            if (boneName.Contains("Arm"))
            {
                center = new Vector3(0, length * 0.5f, 0); // Extend down in Y
            }
            else if (boneName.Contains("Leg"))
            {
                center = new Vector3(0, length * 0.5f, 0); // Extend down in Y
            }
            else if (boneName == "Head")
            {
                center = new Vector3(0, length * 0.3f, 0); // Extend up from neck
            }
            else
            {
                center = Vector3.zero; // Torso bones stay centered
            }
        }
        
        col.center = center;
        
        Debug.Log($"Set collider for {boneName}: Height={col.height:F3}, Radius={col.radius:F3}, Direction={direction}, Center={col.center}");
    }
}