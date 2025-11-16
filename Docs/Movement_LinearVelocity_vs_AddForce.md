# Direct Velocity vs AddForce for Platformer Movement

## TL;DR
For tight, responsive platformer controls, set Rigidbody velocity directly for horizontal movement and use an impulse for jumps. Reserve AddForce for physics-driven interactions (knockback, wind, explosions, pushable objects).

- Horizontal move: set rb.velocity.x every physics step
- Jump: rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse)
- Why: Immediate response, no sliding, predictable speed, common platformer practice

## Why direct velocity feels better

1. Immediate responsiveness
   - AddForce applies acceleration; movement ramps up and feels sluggish.
   - Setting velocity responds instantly to input on press/release.

2. Prevents sliding
   - With AddForce only when pressing keys, inertia keeps the player moving after release.
   - For platformers, we want the character to stop on a dime when input is zero.

3. Consistent, predictable speed
   - Direct velocity sets exact speed regardless of mass, drag, timestep or framerate.
   - AddForce depends on mass and integrates over time, making tuning harder.

4. Established practice
   - Most precision platformers use direct velocity control for tight handling (e.g., Celeste-like feel).

## Recommended pattern (3D)

```csharp
// In FixedUpdate
float inputX = Input.GetAxisRaw("Horizontal");
Vector3 v = rb.velocity;          // use rb.linearVelocity if using Unity Physics package
v.x = inputX * moveSpeed;         // direct horizontal control
rb.velocity = v;                  // preserve Y/Z for gravity/jumps

if (wantJump && isGrounded)
{
    rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
}
```

Notes:
- Read input in Update, apply physics in FixedUpdate.
- Preserve existing Y velocity so gravity and jump fall feel natural.
- Optionally apply smoothing/acceleration curves to v.x if you want less instant response.

## When to use AddForce instead
Use AddForce for interactions that should feel physically simulated:
- Knockback from enemies, recoil, explosions (Impulse/VelocityChange)
- Constant forces like wind, fan zones (Force/Acceleration)
- Moving pushable rigidbodies, ice/slippery movement experiments

AddForce modes quick guide:
- Force: F = m·a over time (frame-rate independent when used in FixedUpdate)
- Acceleration: Ignores mass, applies acceleration directly
- Impulse: Instant velocity change proportional to mass
- VelocityChange: Instant velocity change ignoring mass

## Common pitfalls
- Mixing AddForce with direct velocity on the same axis can fight each other. Prefer one approach per axis.
- If you must combine, apply forces on separate axes or accumulate forces and finally set velocity once.
- Ensure rb.freezeRotation is on for box characters to avoid tipping when using velocity control.

## Links
- Unity manual: Rigidbody.AddForce
- Unity manual: Rigidbody.velocity

## Summary
- Use direct velocity for player-controlled horizontal movement in platformers.
- Use AddForce for jump impulses and physics-driven effects.
- This combination yields tight input with believable vertical motion.
