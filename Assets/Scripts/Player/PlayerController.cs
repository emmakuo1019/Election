using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public CharacterController charCon;
    
    public float moveSpeed = 5f;
    public InputActionReference moveAction;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();

        Debug.Log(moveInput);

        charCon.Move(new Vector3(moveInput.x * moveSpeed, 0f, moveInput.y * moveSpeed));

        //Vector3 moveForward = transform.forward * moveInput.y;
        //Vector3 moveSideways = transform.right * moveInput.x;
    }
}
