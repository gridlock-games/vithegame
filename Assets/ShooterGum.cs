using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShooterGum : MonoBehaviour
{

  public GameObject bulletPrefab;
  public Transform bulletPos;
  public float speed = 1000;
    // Start is called before the first frame update
    void Start()
    {
    Rigidbody bulletPrefabRB = GetComponent<Rigidbody>();
    }

  // Update is called once per frame
  void Update()
  {
    if (Input.GetKeyDown(KeyCode.Space))
    { Shoot(); }
    }

    void Shoot()
  {
    GameObject bulletInst = (GameObject) Instantiate(bulletPrefab, bulletPos.position, this.transform.rotation);
    bulletInst.GetComponent<Rigidbody>().velocity = bulletPos.transform.forward * speed;
  }
}
