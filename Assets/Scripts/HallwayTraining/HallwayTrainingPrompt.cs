using UnityEngine;
using UnityEngine.UI;

namespace Vert.Firefighter.Training
{
    public sealed class HallwayTrainingPrompt : MonoBehaviour
    {
        [SerializeField] Text promptText;

        public void Initialize(Text text)
        {
            promptText = text;
        }

        public void SetText(string message)
        {
            if (promptText != null)
                promptText.text = message;
        }
    }
}
