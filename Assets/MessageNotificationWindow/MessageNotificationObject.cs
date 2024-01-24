using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MessageNotificationObject : MonoBehaviour
{
  [SerializeField]
  TextMeshProUGUI messageText;

  [SerializeField]
  Button button1;

  [SerializeField]
  Button button2;

  [SerializeField]
  Button button3;

  [SerializeField]
  Button button4;

  public UnityEvent onButton1Action;
  public UnityEvent onButton2Action;
  public UnityEvent onButton3Action;
  public UnityEvent onButton4Action;


  public void ShowDialogueBox(string message, string button1Text, UnityEvent forButton1Action)
  {
    messageText.text = message;

    button1.GetComponentInChildren<TextMeshProUGUI>().text = button1Text;
    onButton1Action = forButton1Action;
  }

  public void ShowDialogueBox(string message, string button1Text, UnityEvent forButton1Action, string button2Text, UnityEvent forButton2Action)
  {
    messageText.text = message;

    button1.GetComponentInChildren<TextMeshProUGUI>().text = button1Text;
    onButton1Action = forButton1Action;

    button2.gameObject.SetActive(true);
    button2.GetComponentInChildren<TextMeshProUGUI>().text = button2Text;
    onButton2Action = forButton2Action;

  }

  public void ShowDialogueBox(string message, string button1Text, UnityEvent forButton1Action, string button2Text, UnityEvent forButton2Action, string button3Text, UnityEvent forButton3Action)
  {
    messageText.text = message;

    button1.GetComponentInChildren<TextMeshProUGUI>().text = button1Text;
    onButton1Action = forButton1Action;

    button2.gameObject.SetActive(true);
    button2.GetComponentInChildren<TextMeshProUGUI>().text = button2Text;
    onButton2Action = forButton2Action;

    button3.gameObject.SetActive(true);
    button3.GetComponentInChildren<TextMeshProUGUI>().text = button3Text;
    onButton3Action = forButton3Action;
  }

  public void ShowDialogueBox(string message, string button1Text, UnityEvent forButton1Action, string button2Text, UnityEvent forButton2Action, string button3Text, UnityEvent forButton3Action, string button4Text, UnityEvent forButton4Action)
  {
    messageText.text = message;

    button1.GetComponentInChildren<TextMeshProUGUI>().text = button1Text;
    onButton1Action = forButton1Action;

    button2.gameObject.SetActive(true);
    button2.GetComponentInChildren<TextMeshProUGUI>().text = button2Text;
    onButton2Action = forButton2Action;

    button3.gameObject.SetActive(true);
    button3.GetComponentInChildren<TextMeshProUGUI>().text = button3Text;
    onButton3Action = forButton3Action;

    button4.gameObject.SetActive(true);
    button4.GetComponentInChildren<TextMeshProUGUI>().text = button4Text;
    onButton4Action = forButton4Action;
  }

  public void onButton1Pressed()
  {
    onButton1Action.Invoke();
  }
  public void onButton2Pressed()
  {  onButton2Action.Invoke(); }
  public void onButton3Pressed()
  {  onButton3Action.Invoke(); }
  public void onButton4Pressed()
  {  onButton4Action.Invoke(); }

  public void closeDialogue()
  {
    Destroy(this.gameObject);
  }
}
