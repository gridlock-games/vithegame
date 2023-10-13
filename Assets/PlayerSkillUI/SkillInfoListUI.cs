using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SkillInfoListObject
{
  public Sprite imagefile;
  public string TitleText;
  public string DescriptionText;
}
public class SkillInfoListUI : MonoBehaviour
{
  [SerializeField] GameObject contentWindow;
  [SerializeField] SkillInfoIndividual skillInfoObject;
  [SerializeField] List<SkillInfoListObject> skillInfoList = new List<SkillInfoListObject>();

  // Start is called before the first frame update
  void Start()
    {
    GenerateInfoObject();
    }

  void GenerateInfoObject()
  {
    foreach (var item in skillInfoList)
    {
      var generatedObject = Instantiate(skillInfoObject.gameObject, skillInfoObject.gameObject.transform.position, Quaternion.identity);
      generatedObject.GetComponent<SkillInfoIndividual>().AddDetails(item);
      generatedObject.transform.parent = contentWindow.transform;
    }
  }
}
