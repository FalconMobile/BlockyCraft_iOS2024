using UnityEngine;
using VoxelPlay;

public class Player : VoxelPlayPlayer
{
    [Header("Player")]
    [SerializeField] private Animator _animator;

    private VoxelPlayEnvironment _env;

    private float sprint = 0.5f;

    private void Awake()
    {
        VoxelPlayPlayer.instance = this;
    }

    private void Start()
    {
        _env = VoxelPlayEnvironment.instance;
    }

    private void ManageInput()
    {
        VoxelPlayInputController input = _env.input;
        if (!input.enabled)
        {
            return;
        }
        // Smooth sprint
        if (input.GetButton(InputButtonNames.LeftShift))
        {
            if (sprint < 1) sprint += Time.deltaTime;
        }
        else
        {
            if (sprint > 0.5f) sprint -= Time.deltaTime;
        }
        if (input.GetButtonDown(InputButtonNames.Jump))
        {
            _animator.SetTrigger(AnimationKeyword.Jump);
        }
        _animator.SetFloat(AnimationKeyword.Speed, input.verticalAxis * sprint);
    }

    private void LateUpdate()
    {
        ManageInput();
    }
}
