using System.Collections.Generic;
using UnityEngine;

namespace RealmShards.Enemies
{
    public static class ProjectilePool
    {
        private static readonly Stack<EnemyProjectile> Pool = new Stack<EnemyProjectile>(32);
        private static Transform _root;
        private static Sprite _bulletSprite;

        public static void Warm(int count, Sprite bulletSprite = null)
        {
            EnsureRoot();
            if (bulletSprite != null)
                _bulletSprite = bulletSprite;

            for (int i = 0; i < count; i++)
                Pool.Push(CreateNew());
        }

        public static EnemyProjectile Spawn(
            Vector3 position,
            Vector2 direction,
            float speed,
            float lifetime,
            float damage,
            GameObject owner,
            FactionMember ownerFaction,
            Color color)
        {
            EnsureRoot();
            var proj = Pool.Count > 0 ? Pool.Pop() : CreateNew();
            proj.transform.position = position;
            proj.Initialize(direction, speed, lifetime, damage, owner, ownerFaction, _bulletSprite, color);
            return proj;
        }

        public static void Despawn(EnemyProjectile projectile)
        {
            if (projectile == null)
                return;
            projectile.Shutdown();
            Pool.Push(projectile);
        }

        private static void EnsureRoot()
        {
            if (_root != null)
                return;
            var go = new GameObject("EnemyProjectilePool");
            Object.DontDestroyOnLoad(go);
            _root = go.transform;

            if (_bulletSprite == null)
                _bulletSprite = CreateBulletSprite();
        }

        private static EnemyProjectile CreateNew()
        {
            var go = new GameObject("EnemyProjectile");
            go.transform.SetParent(_root);
            go.layer = Core.GameLayers.EnemyProjectile;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 20;
            if (_bulletSprite != null)
                sr.sprite = _bulletSprite;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.12f;

            var proj = go.AddComponent<EnemyProjectile>();
            go.SetActive(false);
            return proj;
        }

        private static Sprite CreateBulletSprite()
        {
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
