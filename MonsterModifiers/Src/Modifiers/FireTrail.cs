using System.Collections.Generic;
using UnityEngine;

namespace MonsterModifiers.Modifiers;

public class FireTrail : MonoBehaviour
{
    private Character _character;
    private Vector3 _lastPosition;
    private float _timer;
    private readonly List<GameObject> _activeEffects = new List<GameObject>();

    private const float SpawnInterval = 0.4f;
    private const float MinMoveDistance = 0.5f;
    private const float EffectLifetime = 1.5f;

    public void Init(Character character)
    {
        _character = character;
        _lastPosition = character.transform.position;
    }

    private void FixedUpdate()
    {
        if (_character == null || _character.IsDead())
            return;

        _timer += Time.fixedDeltaTime;
        if (_timer < SpawnInterval)
            return;

        _timer = 0f;

        Vector3 currentPos = _character.transform.position;
        if (Vector3.Distance(currentPos, _lastPosition) < MinMoveDistance)
            return;

        _lastPosition = currentPos;

        GameObject firePrefab = ZNetScene.instance?.GetPrefab("fx_Hen_Egg_Heat");
        if (firePrefab == null)
            firePrefab = ZNetScene.instance?.GetPrefab("vfx_fire_shortlived");
        if (firePrefab == null)
            return;

        GameObject effect = Instantiate(firePrefab, currentPos, Quaternion.identity);
        if (effect != null)
            Destroy(effect, EffectLifetime);
    }
}
