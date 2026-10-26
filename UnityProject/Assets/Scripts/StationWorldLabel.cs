using UnityEngine;

namespace LastLight
{
    public sealed class StationWorldLabel : MonoBehaviour
    {
        public Shader textShader;
        TextMesh text;
        MeshRenderer meshRenderer;
        Camera viewer;
        Material material;
        void Awake()
        {
            text=GetComponent<TextMesh>();meshRenderer=GetComponent<MeshRenderer>();viewer=Camera.main;
            material=new Material(textShader);meshRenderer.sharedMaterial=material;
            text.font.RequestCharactersInTexture(text.text,text.fontSize);
            UpdateAtlas(text.font);
            meshRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;meshRenderer.receiveShadows=false;
        }
        void OnEnable() {Font.textureRebuilt+=UpdateAtlas;}
        void OnDisable() {Font.textureRebuilt-=UpdateAtlas;}
        void UpdateAtlas(Font font) {if(text!=null&&material!=null&&font==text.font)material.mainTexture=font.material.mainTexture;}
        void LateUpdate()
        {
            if(viewer==null)return;
            meshRenderer.enabled=(viewer.transform.position-transform.position).sqrMagnitude<900;
            transform.rotation=viewer.transform.rotation;
        }
        void OnDestroy() {if(material!=null)Destroy(material);}
    }
}
