using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillInfoIndividual : MonoBehaviour
{

  [SerializeField] Text titleText;
  [SerializeField] Text descriptionText;
  [SerializeField] Image iconText;
  // Start is called before the first frame update

  //SkillInfoIndividual(SkillInfoListObject silo)
  //{
  //  descriptionText.text = silo.TitleText;
  //  titleText.text = silo.TitleText;
  //  iconText.sprite = silo.imagefile;
  //}

  public void AddDetails(SkillInfoListObject silo)
  {
    descriptionText.text = silo.DescriptionText;
    titleText.text = silo.TitleText;
    iconText.sprite = silo.imagefile;
  }
}
