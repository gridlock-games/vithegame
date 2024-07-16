using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Vi.Core
{
	[RequireComponent(typeof(Slider))]
	public class SliderEndEditEvent : MonoBehaviour, IPointerUpHandler
	{
		public event UnityAction<float> EndDrag;

		private Slider slider;
		private void Awake()
        {
			slider = GetComponent<Slider>();
        }

		public void OnPointerUp(PointerEventData data)
		{
			EndDrag.Invoke(slider.value);
		}
	}
}