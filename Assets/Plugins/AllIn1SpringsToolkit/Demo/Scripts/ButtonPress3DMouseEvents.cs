using UnityEngine;
using UnityEngine.EventSystems;

namespace AllIn1SpringsToolkit.Demo.Scripts
{
    public class ButtonPress3DMouseEvents : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private ButtonPress3DController buttonPress3DController;

		public void OnPointerDown(PointerEventData eventData)
		{
			buttonPress3DController.MouseDown();
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			buttonPress3DController.MouseUp();
		}
    }
}