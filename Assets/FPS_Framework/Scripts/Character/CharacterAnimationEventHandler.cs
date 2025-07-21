using UnityEngine;

public class CharacterAnimationEventHandler : MonoBehaviour
{
    [SerializeField]
    private CharacterBehaviour playerCharacter;

    private void OnAnimationEndedHolster()
    {
        if (playerCharacter != null)
            playerCharacter.AnimationEndedHolster();
    }

    private void OnEjectCasing()
    {
        //Notify the character.
        //if (playerCharacter != null)
        //    playerCharacter.EjectCasing();
    }
    private void OnAmmunitionFill(int amount = 0)
    {
        //Notify the character.
        if (playerCharacter != null)
            playerCharacter.FillAmmunition(amount);
    }
    private void OnAnimationEndedReload()
    {
        //Notify the character.
        if (playerCharacter != null)
            playerCharacter.AnimationEndedReload();
    }
    private void OnSlideBack(int back)
    {
    }
}
