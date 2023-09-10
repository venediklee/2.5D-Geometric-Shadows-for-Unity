using UnityEngine;

namespace GoblinsInteractive.DateTimeLightSystem
{
    [ExecuteAlways]
    public sealed class LightSamplePositionSetter : MonoBehaviour
    {
        const string _lightSampleLeftChildName = "LightSampleLeftPoint";
        const string _lightSampleRightChildName = "LightSampleRightPoint";

        static readonly int _leftID = Shader.PropertyToID("_LightSampleLeftPoint");
        static readonly int _rightID = Shader.PropertyToID("_LightSampleRightPoint");

        void Awake()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                SetMaterialProperties();
            }
            else
            {
                SetPropertyBlock();
            }
#else
            SetMaterialProperties();
#endif
        }

        /// <summary>
        /// Creates a new material instance; SRP batching compatible<br></br>
        /// Will leak materials into the scene if used in edit mode
        /// </summary>
        void SetMaterialProperties()
        {
            var renderer = GetComponent<Renderer>();
#if UNITY_EDITOR
            renderer.SetPropertyBlock(null);
#endif

            //create a new material instance to use SRP batching
            var material = renderer.material;

            var left = transform.Find(_lightSampleLeftChildName);
            var right = transform.Find(_lightSampleRightChildName);
            material.SetVector(_leftID, (Vector2)left.position);
            material.SetVector(_rightID, (Vector2)right.position);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Sets the properties per renderer; breaks SRP batching; doesn't create a new material instance<br></br>
        /// Only used in editor in edit mode
        /// </summary>
        void SetPropertyBlock()
        {
            var renderer = GetComponent<Renderer>();
            var left = transform.Find(_lightSampleLeftChildName);
            var right = transform.Find(_lightSampleRightChildName);

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetVector(_leftID, (Vector2)left.position);
            block.SetVector(_rightID, (Vector2)right.position);

            renderer.SetPropertyBlock(block);
        }

        void Update()
        {
            if (transform.hasChanged)
            {
                SetPropertyBlock();
                transform.hasChanged = false;
            }
        }

        //Add light sample point children 
        void Reset()
        {
            if (!transform.Find(_lightSampleLeftChildName))
            {
                new GameObject(_lightSampleLeftChildName).transform.SetParent(this.transform, false);
            }
            if (!transform.Find(_lightSampleRightChildName))
            {
                new GameObject(_lightSampleRightChildName).transform.SetParent(this.transform, false);
            }
        }
#endif
    }
}
