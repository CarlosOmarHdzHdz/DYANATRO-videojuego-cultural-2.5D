using UnityEngine;

// Art scale in world units. Keep the player's physics/movement unchanged and
// size the environment from the same visible height used by its sprite atlas.
public static class XunjuuWorldScale
{
    public const float PlayerLocalHeight = 1.62f;
    public const float HouseDoorHeight = 2.42f;
    public const float DoorToPlayer = 1.25f;
    public const float MinimumTreeToPlayer = 3.2f;
    public const float MaximumTreeToPlayer = 4.8f;

    public static float PlayerHeight(Transform player)
        => PlayerLocalHeight * (player != null ? Mathf.Abs(player.lossyScale.y) : 1f);

    public static float HouseFactor(Transform player)
        => PlayerHeight(player) * DoorToPlayer / HouseDoorHeight;

    public static float TreeHeight(Transform player, float variation)
        => PlayerHeight(player) * Mathf.Lerp(MinimumTreeToPlayer, MaximumTreeToPlayer, Mathf.Clamp01(variation));

    public static void SetUniformWorldScale(Transform target, float scale)
    {
        Vector3 inherited = target.parent != null ? target.parent.lossyScale : Vector3.one;
        target.localScale = new Vector3(scale / NonZero(inherited.x), scale / NonZero(inherited.y), scale / NonZero(inherited.z));
    }

    private static float NonZero(float value) => Mathf.Abs(value) > .0001f ? value : 1f;
}
