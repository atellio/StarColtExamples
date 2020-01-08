/*
This code controls the launching and landing of dolls in the tower stacking game, Oshka
I'm proud of this code because of its dynamic blending between physics states (kinematic and dynamic) 
for handling doll launching and landing.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Prime31.ZestKit;

public class DollControl : MonoBehaviour
{
    public DollData.Doll dollType;
    public float pivoteHalfArc = 20f;
    public float pivotRate = 2f;
    public Transform sprite;
    public Transform childSpawn;
    public Transform childLock;
    public PolygonCollider2D polyCollider;
    public ParticleSystem trailParticles;

    EaseType easingType = EaseType.BackInOut;
    TrailRenderer trail;
    DollAnimationController dollAnimControl;
    DollPool parentPool;
    Rigidbody2D rigBody;
    Transform parentLock;
    Vector2 storedPosition;
    DollSFX dollSFX;
    bool isPivoting = false;
    bool shouldLand;
    bool isShooting;
    bool isFollowingParentLock;
    bool hasDoneFallStuff;
    float currentPivotTime = 0f;
    float directionMod = 1f;
    float pivotStartDelay = 0.5f;
    int stationaryLayer = 8;
    int shootingLayer = 9;
    int scoreValue = 1;


    private void Awake()
    {
        trail = GetComponentInChildren<TrailRenderer>();
        rigBody = GetComponent<Rigidbody2D>();
        dollAnimControl = GetComponent<DollAnimationController>();
        dollSFX = GetComponentInChildren<DollSFX>();
    }

    private void Update()
    {
        if(isShooting)
        {
            // If this doll has started to fall, set it's physics layer so that it can collide with already stacked dolls
            if(IsVelocityNegativeY() && gameObject.layer != stationaryLayer)
                SetLayerRecursively(gameObject, stationaryLayer);

            // If we are falling on a failed launch, trigger fail activities
            if (IsVelocityNegativeY() && !shouldLand && !hasDoneFallStuff)
            {
                hasDoneFallStuff = true;
                GameManager.Instance.SetState(GameManager.GameStates.GameOver);
                // Collapse all dolls already in the doll tower
                DollManager.Instance.CollapseTower();
                dollSFX.PlayFailSound();
            }
        }

        // If we've landed, lock the position of this doll to the previous landed doll to create a tower of dolls
        if (isFollowingParentLock && DollManager.Instance.IsLastActiveDoll(this) && parentLock != null)
            transform.position = parentLock.transform.position;
    }

    // This is called by GameManager if this doll is the first doll of a new game
    public void DoStartDollStuff()
    {
        ResetRotation();
        SetActive(true);
        SetIsPivoting(true);
        TriggerIdleFace();
    }

    public void SetActive(bool state)
    {
        gameObject.SetActive(state);
        if(state == false) parentPool.ReturnObject(this);
    }

    // Is the doll falling?
    public bool IsVelocityNegativeY()
    {
        return rigBody.velocity.y < 0f;
    }

    // Set if the doll should land successfully or if this was a failed jump (called by WobbleMaster)
    public void SetShouldLand(bool state)
    {
        shouldLand = state;
    }

    public void StoreCurrentPosition()
    {
        storedPosition = transform.position;
    }

    // After gameover, if we continue, use this to return the doll to its position before falling
    public void ReturnToStoredPosition()
    {
        transform.position = storedPosition;
    }

    public void DoGameOverStuff()
    {
        shouldLand = false;
        StoreCurrentPosition();
        isFollowingParentLock = false;
        SetIsPivoting(false);
        SetDynamic(true);
    }

    // Set whether the doll is using dynamic or kinematic physics
    // e.g. if we are jumping/falling, use dynamic physics
    // e.g. if we have landed, switch to kinematic physics
    // if we are dynamic, don't follow the parent lock of the previous doll
    public void SetDynamic(bool state)
    {
        rigBody.isKinematic = !state;

        if(state)
        {
            isFollowingParentLock = false;
            SetIsPivoting(false);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(DollManager.Instance.IsLastActiveDoll(this))
        {
            // If we are falling after a successful jump and we hit another doll, do landing stuff
            if(IsVelocityNegativeY() && collision.transform.tag == "Doll" && !isPivoting && shouldLand)
            {
                DoLandingStuff(collision);
            }
        }
    }

    // Triggered after a successful jump on collision with the previously landed doll
    void DoLandingStuff(Collision2D collision)
    {
        ResetRotation();
        PlayState.Instance.AddScore(scoreValue); 

        // Lock our position to the previous doll
        parentLock = collision.gameObject.GetComponent<DollControl>().GetChildLock();
        isFollowingParentLock = true;

        // Trigger the squashing animation of the previous doll
        collision.gameObject.GetComponent<DollAnimationController>().TriggerSquash();

        // Trigger animations for this doll
        dollAnimControl.TriggerFaceIdle();
        dollAnimControl.TriggerLandingAnim();

        if(trail != null) trail.enabled = false;
        dollSFX.PlayLandSound();
        rigBody.isKinematic = true;
        StartCoroutine(WaitThenPivot());

        // Check to see if this jump unlocked a secret
        SecretUnlocks.Instance.CheckForCharacterUnlock(transform.position.y);
    }

    IEnumerator WaitThenPivot()
    {
        yield return new WaitForSeconds(pivotStartDelay); 
        SetIsPivoting(true);
    }

    // Trigger opening animations of this doll when the next doll is launched
    public void DoOpenStuff()
    {
        dollAnimControl.TriggerFaceIdle();
        dollAnimControl.TriggerOpen();
        dollSFX.PlayOpenSound();   
    }

    private void OnDisable()
    {
        transform.position = Vector3.zero;
        ResetRotation();
        currentPivotTime = 0f;
        shouldLand = false;
        isShooting = false;
        isFollowingParentLock = false;
        parentLock = null;
        gameObject.layer = stationaryLayer;
        polyCollider.enabled = true;
        hasDoneFallStuff = false;

        if (trail != null)
        {
            trail.Clear();
            trail.enabled = false;
        }
    }

    // Set the rate at which this doll pivots while stationary - based on current difficulty
    public void SetPivotRate(float rate)
    {
        pivotRate = rate;
    }

    public void ResetRotation()
    {
        if(rigBody != null)
        {
            rigBody.velocity = Vector2.zero;
            rigBody.angularVelocity = 0f;
        }
        
        transform.eulerAngles = Vector3.zero;
        sprite.localRotation = Quaternion.Euler(0f, 0f, 0f);
    }

    // Launch the current doll
    public void Shoot(float shootForce)
    {
        // Set this dolls physics layer to the shooting layer (preventing weird physics interactions with stationary dolls on launch)
        SetLayerRecursively(gameObject, shootingLayer);

        // Stop this doll from pivoting
        SetIsPivoting(false);

        // Add physics forces
        rigBody.isKinematic = false;
        rigBody.AddRelativeForce(Vector2.up * shootForce, ForceMode2D.Impulse);
        isShooting = true;

        // Trigger animations
        dollAnimControl.TriggerFaceHappy();
        dollAnimControl.TriggerLaunchAnim();

        if (trail != null)
        {
            trail.Clear();
            trail.enabled = true;
        }

        if(trailParticles != null) trailParticles.Play();
        dollSFX.PlayJumpSound();
    }

    // Set whether this doll is pivoting once landed
    public void SetIsPivoting(bool state)
    {
        isPivoting = state;
        
        // Choose a random direction to start pivoting in so dolls dont always pivot in the same direction
        directionMod = GetRandomDirectionMod();

        if (state)
        {
            isShooting = false;
            rigBody.isKinematic = true;
            rigBody.velocity = Vector2.zero;  
            rigBody.angularVelocity = 0f;
            StartCoroutine(Pivot());
        }
        else
        {
            StopCoroutine("Pivot");
        }
    }

    public float GetDirectionMod()
    {
        return directionMod;
    }

    public bool IsPivoting()
    {
        return isPivoting;
    }

    // Code for handling how the doll pivots once landed
    IEnumerator Pivot()
    {
        float totalPivotTime = 0f;
        currentPivotTime = pivotRate * 0.5f;
        int pivotTrack = 0;

        while (isPivoting)
        {
            // Only pivot if we're in the Play state
            if (GameManager.Instance.DoesCurrentStateEqual(GameManager.GameStates.Play))
            {
                currentPivotTime += Time.deltaTime;
                var t = Mathf.PingPong(currentPivotTime, pivotRate);
                var phase = Zest.ease(EaseType.SineInOut, -1f, 1f, t, pivotRate);
                sprite.localRotation = Quaternion.Euler(new Vector3(0f, 0f, phase * pivoteHalfArc * directionMod));
                var numFullPivots = (currentPivotTime - pivotRate * 0.5f) / (pivotRate * 2f);

                // Update the number of times this doll has pivoted (this determines how much time the player has left to launch the doll) 
                if (WobbleMaster.Instance != null && GameManager.Instance.DoesCurrentStateEqual(GameManager.GameStates.Play))
                    WobbleMaster.Instance.UpdateWobbleCount(numFullPivots);
            }

            yield return null;
        }
    }

    // Set a reference for the pool this doll belongs to
    public void SetParentPoolTo(DollPool pool)
    {
        parentPool = pool;
    }

    // Go through child transforms of this doll and set the physics layer of each
    public static void SetLayerRecursively(GameObject go, int layerNumber)
    {
        if (go == null) return;

        foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = layerNumber;
        }
    }

    float GetRandomDirectionMod()
    {
        if (Random.value < 0.5f)
            return 1f;
        else
            return -1f;
    }

    public Vector3 GetChildSpawnPos()
    {
        return childSpawn.position;
    }

    public Transform GetChildLock()
    {
        return childLock;
    }

    public Vector3 GetSpriteRotation()
    {
        return sprite.eulerAngles;
    }

    public Transform GetSpriteTransform()
    {
        return sprite.transform;
    }

    public bool IsLanded()
    {
        return isPivoting;
    }
}
