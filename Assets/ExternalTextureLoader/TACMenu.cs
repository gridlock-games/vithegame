using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TACMenu : MonoBehaviour
{
  [SerializeField] private bool isPlayerAgreedBefore; //Temp Code
  [SerializeField] private GameObject agreementModeObject;
  [SerializeField] private GameObject readingModeObject;

  [SerializeField] private Button agreedButton;
  [SerializeField] private Button disagreedButton;
  [SerializeField] private Button okButton;

  [SerializeField] private TextMeshProUGUI termsTitle;
  [SerializeField] private TextMeshProUGUI termsField;

  [SerializeField] private string termsID;

  // Start is called before the first frame update
  private void Start()
  {
    agreementCheck();
    startFileRetreval();
    assignButtons();
  }

  // Update is called once per frame
  private void Update()
  {
  }

  private void agreementCheck()
  {
    //Add code that check if player had accepted or not

    //Check if the player has agreed Before
    if (isPlayerAgreedBefore)
    {
      agreementModeObject.SetActive(false);
      readingModeObject.SetActive(true);
    }
    else
    {
      agreementModeObject.SetActive(true);
      readingModeObject.SetActive(false);
    }
  }

  private void startFileRetreval()
  {
    ExternalFileLoaderWeb.DoTextWebRequestID(termsID, updateTermsData);
  }

  private void assignButtons()
  {
    agreedButton.onClick.AddListener(onAgreed);
    disagreedButton.onClick.AddListener(onDisagreed);
    okButton.onClick.AddListener(onOkAccept);
  }

  private void updateTermsData(string terms)
  {
    termsTitle.text = "Vi Terms and condition";
    termsField.text = terms;
  }

  private void onAgreed()
  {
    Debug.Log("User agreed to terms");
  }

  private void onDisagreed()
  {
    Debug.Log("User disagreed to terms");
  }

  private void onOkAccept()
  {
    Debug.Log("User just reading what he agreed");
  }
}