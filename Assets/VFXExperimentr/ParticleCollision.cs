using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ParticleCollision : MonoBehaviour
{
  //This move forward
  Rigidbody rb;
  Vector3 StartPos;
  private void Start()
  {
    StartPos = this.transform.position;

    //rb = this.gameObject.GetComponent<Rigidbody>();
    //rb.AddForce(Vector3.forward * 100);
  }

  private void FixedUpdate()
  {
    if (Vector3.Distance(StartPos, this.transform.position) >= 5)
    {
      //Do something
    }
  }
  private void OnTriggerEnter(Collider other)
  {
    if (other != null)
    {
      Debug.Log("HIT");
    }
  }
}
