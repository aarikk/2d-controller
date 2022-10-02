using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Controller2D : MonoBehaviour
{
    private BoxCollider2D boxCollider;

    const float skinWidth = .001f;

    // Maximium climbing angle in Degrees
    public float maxClimbAngle = 80f;

    // Collision Setup
    // Horizontal and vertical raycast counts (spacing is calculated dynamically
    // Min Ray num is 2 (there's a check in the recast handler functions

    public int horizontalRayCount = 4;
    public int verticalRayCount = 4;
    float horizontalRaySpacing;
    float verticalRaySpacing;

    // Struct Definition
    private RaycastOrigins raycastOrigins;
    public LayerMask collisionMask;

    // State Machine (Collision Info) - > This should be the basis for a state machine in the future.
    public CollisionInfo collisionsInfo;

    void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        CalculateRaySpacing();
    }

    // Move function moves the player - is called from the PlayerInput controller's update function
    #region Move Function
    public void Move(Vector3 velocity)
    {
        
        UpdateRaycastOrigins(); // Update Raycast origins to fit the new position of the player
        collisionsInfo.Reset(); // Resets all the states (sets everything to false) 

        // Checks for collisions (Unless no movement)
        if (velocity.x != 0) HorizontalCollisions(ref velocity);
        if (velocity.y != 0) VerticalCollisions(ref velocity);

        // Moves Player
        transform.Translate(velocity);
    }
    #endregion

    // Horizontal and Vertical Collision functions (might need to either merge it or refactor
    #region Horizontal Collision
    void HorizontalCollisions(ref Vector3 velocity)
    {
        float directionX = Mathf.Sign(velocity.x); // Get the direction of the movement 1 (right) or -1 (left)
        float rayLength = Mathf.Abs(velocity.x) + skinWidth; // Sets the raylength to new position + skin width

        for (int i = 0; i < horizontalRayCount; i++)
        {
            // Deterimn which rayOrigin to use: if the player is moving left than bottom left, else bottomRight
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;

            // Since the left and right rays are propegated bottom -> up this row will add the spacing value
            // to every i value (0 no spacing, 1 * spacing * (0,1) etc.. 
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);

            //Builds the ray and checks for hits on the correct side. 
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);
            Debug.DrawRay(rayOrigin, Vector3.right * directionX * rayLength, Color.red);

            if (hit)
            {
                // Get the angle between the element we colided with and the world normal (Vector2.up)
                // We want this angle in case we will decide to enable climbing up a slope (See climb slope function below)
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                if (i == 0 && slopeAngle <= maxClimbAngle) //If this is the first colllision - and the slope is climbable
                {
                    float distanceToSloapeStart = 0;
                    if(slopeAngle != collisionsInfo.slopeAngleOld) // If the player collided with a new slope
                    {
                        distanceToSloapeStart = hit.distance - skinWidth; // Get the distnace to the new slope (reducing skin width)
                        velocity.x -= distanceToSloapeStart * directionX; // Use velocity at the slope
                    }

                    ClimbSlope(ref velocity, slopeAngle); //Adjust velocity to move up the slope 
                    //If after calculating the new direction - move the player back onto the slope. 
                    velocity.x += distanceToSloapeStart * directionX;
                }
                if (!collisionsInfo.climbingSlope || slopeAngle > maxClimbAngle)
                {
                    velocity.x = (hit.distance - skinWidth) * directionX;
                    rayLength = hit.distance;
                    if (collisionsInfo.climbingSlope) {
                        // Calculate the right X velocity if while climbing we encounter a horizontal unclimbable barrier.
                        // slopeAngle = theta 
                        // Velocity Y is of corse the Y position. 
                        // The original equasion is Tan(theta) = Y/X
                        // Y / Tan(theta)  = X position
                        velocity.y = Mathf.Tan(collisionsInfo.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
                    }
                    collisionsInfo.left = directionX == -1;
                    collisionsInfo.right = directionX == 1;
                }
            }
        }
    }
    #endregion
    #region Vertical Collision
    void VerticalCollisions(ref Vector3 velocity)
    {

        float directionY = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + skinWidth;


        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);
            Debug.DrawRay(rayOrigin, Vector3.up * directionY * rayLength, Color.red);

            if (hit)
            {
                velocity.y = (hit.distance * skinWidth) * directionY;
                rayLength = hit.distance;
                
                if (collisionsInfo.climbingSlope) {

                    // Calculate the right X velocity if while climbing we encounter a vertical collision
                    // slopeAngle = theta 
                    // Velocity Y = y. 
                    // The original equasion is Tan(theta) = Y/X
                    // Y / Tan(theta)  = X position
                    velocity.x = velocity.y / Mathf.Tan(collisionsInfo.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x);
                }
                collisionsInfo.below = directionY == -1;
                collisionsInfo.above = directionY == 1;
            }
        }
        if (collisionsInfo.climbingSlope)
        {
            float directionX = Mathf.Sign(velocity.x);
            rayLength = Mathf.Abs(velocity.x) + skinWidth;
            Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * velocity.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if(hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != collisionsInfo.slopeAngle)
                {
                    velocity.x = (hit.distance - skinWidth) * directionX;
                    collisionsInfo.slopeAngle = slopeAngle;
                }

            }
        }
    }
    #endregion

    // Climb Slope funciton is used in both the vertical and horizontal collision functions.
    #region Climp Slope Function
    void ClimbSlope(ref Vector3 velocity, float slopeAngle)
    {
        // This function uses trigonometry to find the Y and X positions
        // slopeAngle = theta
        // moveDistance = Hypotenuse
        // Sin(theta) * Hypotenuse = Y position (opposite to the Hypotenuse) 
        // Cos(theta) * Hypotenuse = X position (adjacent to the Hypotenuse)
        // Remember - to use angles we must convert the Angles (degrees) to radians Mathf.Deg2Rad

        float moveDistance = Mathf.Abs(velocity.x); // Get the distnace (or Hypotenuse) 
        float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance; // Get Y vector using formula above
        if (velocity.y <= climbVelocityY) //If the calculated Y velocity is larger than current (i.e player is not jumping)
        {
            velocity.y = climbVelocityY; 
            velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x); // Get new X with formula
            collisionsInfo.below = true;
            collisionsInfo.climbingSlope = true;
            collisionsInfo.slopeAngle = slopeAngle;
        }
    }

    #endregion

    // Setup Raycast origins and rays.
    #region Raycast Handlers
    void UpdateRaycastOrigins()
    {
        Bounds bounds = boxCollider.bounds;
        bounds.Expand(skinWidth * -2);

        raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    void CalculateRaySpacing()
    {
        Bounds bounds = boxCollider.bounds;
        bounds.Expand(skinWidth * -2); // Shrink the bounds by the width of the skin * -2 (we want to shrink that's why minus)

        // Make sure we  
        horizontalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);
        verticalRayCount = Mathf.Clamp(verticalRayCount, 2, int.MaxValue);

        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);

    }
  
    struct RaycastOrigins
    {
       public Vector2 topLeft, topRight, bottomLeft, bottomRight;
    }
    #endregion

    // State Machine Definition - to be refactored
    #region State Machine (collision info struct)
    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;
        public bool climbingSlope;
        public float slopeAngle;
        public float slopeAngleOld; 
        public void Reset()
        {
            above = below = left = right = climbingSlope = false;
            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }

    #endregion
}
