using UnityEngine;

public class Entity : MonoBehaviour
{

    public float speed;
    public float jump;

    protected new Rigidbody rigidbody;

    public virtual void Awake() {
        
        rigidbody = GetComponent<Rigidbody>();

    }

    public void Jump() {

        rigidbody.AddForce(Vector3.up * jump, ForceMode.Impulse);

    }

}