using UnityEngine;
using System.Collections;

public class VoTri : WalkerPassive
{
    [Header("Player Detect")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectRange = 3f;

    [Header("Question Mark")]
    [SerializeField] private GameObject questionMark;
    [SerializeField] private float surpriseTime = 0.4f;
    [SerializeField] private float questionTime = 1f;

    private bool isRunningAway = false;
    private bool isSurprised = false;

    protected override void Start()
    {
        base.Start();

        if (questionMark != null)
            questionMark.SetActive(false);
    }

    protected override void FixedUpdate()
    {
        if (isSurprised)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        CheckPlayer();

        base.FixedUpdate();
    }

    private void CheckPlayer()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= detectRange)
        {
            if (!isRunningAway)
            {
                FaceAwayFromPlayer();
                isRunningAway = true;
            }
        }
        else
        {
            isRunningAway = false;
        }
    }

    private void FacePlayer()
    {
        bool playerOnRight = player.position.x > transform.position.x;

        if (playerOnRight && !movingRight)
            ChangeDirection();
        else if (!playerOnRight && movingRight)
            ChangeDirection();
    }

    private void FaceAwayFromPlayer()
    {
        bool playerOnRight = player.position.x > transform.position.x;

        if (playerOnRight && movingRight)
            ChangeDirection();
        else if (!playerOnRight && !movingRight)
            ChangeDirection();
    }

    public void OnHit()
    {
        StopAllCoroutines();
        StartCoroutine(SurprisedRoutine());
    }

    private IEnumerator SurprisedRoutine()
    {
        isSurprised = true;

        rb.linearVelocity = Vector2.zero;

        // Quay mặt nhìn player
        FacePlayer();

        yield return new WaitForSeconds(surpriseTime);

        if (questionMark != null)
            questionMark.SetActive(true);

        yield return new WaitForSeconds(questionTime);

        if (questionMark != null)
            questionMark.SetActive(false);

        // Quay đầu chạy tiếp
        FaceAwayFromPlayer();

        isSurprised = false;
        isRunningAway = true;
    }
}