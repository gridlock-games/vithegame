using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ParticleCollision : MonoBehaviour
{
  //This move forward
  Rigidbody rb;

  private void Start()
  {

    //rb = this.gameObject.GetComponent<Rigidbody>();
    //rb.AddForce(Vector3.forward * 100);
  }

  private void OnTriggerEnter(Collider other)
  {
    if (other != null)
    {
      Debug.Log("HIT");
    }
  }
}
