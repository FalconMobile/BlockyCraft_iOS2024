using System.Collections.Generic;
using MobileTouchInput;
using UnityEngine;
using UnityEngine.EventSystems;
using VoxelPlay;

public class ButtonEvents : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private MobileInput mobileInput;
    [SerializeField] private InputButtonNames buttonName;

    private bool _longPressTriggered;
    private float _timePressStarted;
    private bool _isPointerDown;
    private bool _isInputDownButton1;
    private bool _isInputDown;

    private void Update()
    {
        if (_isPointerDown && !_longPressTriggered)
        {
            _longPressTriggered = true;
                mobileInput.OnButtonPressed(buttonName);
        }
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        _isPointerDown = true;
        _longPressTriggered = false;
        
        if (InputButtonNames.Button1 == buttonName)
        {
            _isInputDownButton1 = true;
            StartCoroutine(IntervalClickButton());
            return;
        }

        if (_isInputDown == false)
        {
            _isInputDown = true;
            mobileInput.OnButtonDown(buttonName);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPointerDown = false;
        _isInputDown = false;
        _isInputDownButton1 = false;
        
        mobileInput.OnButtonUp(buttonName, _timePressStarted);
    }

    private IEnumerator<WaitForSeconds> IntervalClickButton()
    {
        for (;;)
        {
            if (_isInputDownButton1)
            {
                mobileInput.OnButtonDown(buttonName);
                yield return Yielders.WaitForSeconds(0.3f);
            }
            else
            {
                break;
            }
        }
    }
}