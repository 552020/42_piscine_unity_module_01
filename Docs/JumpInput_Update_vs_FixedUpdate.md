# Why set `wantJump` in Update and consume it in FixedUpdate?

When using Unity’s built-in input (Input.GetKeyDown / Input.GetButtonDown), the input signal exists for **one render frame**. Physics, however, runs on the **FixedUpdate** timestep, which can occur at a different rate and not necessarily every frame. If you only check jump input inside FixedUpdate, you can easily **miss** the one-frame pulse.

## The problem
- `GetKeyDown` is true for a single frame in `Update`.
- `FixedUpdate` might not run on that exact frame (e.g., 60 FPS rendering, 50 Hz physics). If the jump press happens between physics ticks, the check inside `FixedUpdate` never sees it.
- Result: "I pressed jump but nothing happened" — intermittent, hard-to-reproduce misses.

## The pattern that fixes it
1. In `Update`: detect the edge-triggered input and set a flag (latch it).
2. In `FixedUpdate`: read and consume that flag when applying physics (forces/velocity changes).

```csharp
// Update: capture the one-frame input pulse
void Update()
{
    if (this == activePlayer && Input.GetKeyDown(KeyCode.Space))
        wantJump = true; // latch
}

// FixedUpdate: consume the latched input when safe to modify physics
void FixedUpdate()
{
    if (isGameOver || activePlayer != this || rb == null) return;

    // ... movement ...

    isGrounded = CheckGrounded();
    if (wantJump && isGrounded)
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

    wantJump = false; // consume the latch so we don’t double-jump
}
```

## Why it works
- `Update` runs every rendered frame, so it reliably sees `GetKeyDown`.
- The flag bridges the timing gap to `FixedUpdate`, ensuring the input is not lost.
- Physics changes still happen in `FixedUpdate`, keeping simulation stable and deterministic.

## Extras you can add later
- Jump buffer: keep `wantJump` true for a very short window (e.g., 0.1s) so late presses still fire when you hit the ground.
- Coyote time: allow a tiny grace period (e.g., 0.1s) after leaving a ledge where jump is still permitted.

These polish features use the same idea: store intent with a timer, then consume in `FixedUpdate` when conditions are met.

## Summary
- Set `wantJump` in `Update` to capture the one-frame `GetKeyDown` pulse.
- Consume it in `FixedUpdate` when applying physics.
- This prevents missed jumps and keeps physics changes in the correct loop.
