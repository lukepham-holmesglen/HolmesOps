using UnityEngine;

namespace Mirza.AERO
{
    // May require domain reloading to be enabled.
    // Scene mode can apparently mess it up, otherwise.

    // So at least hiding scene views and then playing
    // will fix the updater and it will work as normal.

    [ExecuteAlways]
    public class CustomRenderTextureUpdater : MonoBehaviour
    {
        public CustomRenderTexture texture;

        void Update()
        {
            texture.Update(1);
        }
    }
}
