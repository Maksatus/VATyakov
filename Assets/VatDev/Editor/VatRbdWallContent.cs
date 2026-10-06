using System;
using UnityEngine;
using Random = System.Random;

namespace VATyakov.Dev
{
    internal static class VatRbdWallContent
    {
        private const string Path = "Assets/VatDev/Content/RBDDestroy/rbd_wall.abc";
        private const float Fps = 30f;
        private const int Frames = 150;
        private const int Columns = 25;
        private const int Rows = 18;
        private const int DebrisCount = 50;
        private const float BrickSize = 0.2f;
        private const float DebrisSize = 0.08f;
        private const float BlastTime = 0.5f;
        private const float Gravity = 9.8f;
        private const float MinSpeed = 2f;
        private const float MaxSpeed = 6f;
        private const float DebrisSpeed = 9f;
        private const float MinTurnRate = 90f;
        private const float MaxTurnRate = 720f;
        private const float DespawnShare = 0.3f;
        private const float BrickDespawn = 3.5f;
        private const float DebrisDespawn = 2f;
        private const float DespawnSpread = 1.5f;
        private const int Seed = 15;

        private static readonly Vector3 _blastCenter = new(0f, 1.5f, -1f);

        public static void Regenerate()
        {
            var random = new Random(Seed);
            var root = new GameObject("rbd_wall");
            var mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var pieces = new Piece[Columns * Rows + DebrisCount];
            for (var row = 0; row < Rows; row++)
            {
                for (var column = 0; column < Columns; column++)
                {
                    var start = new Vector3((column - (Columns - 1) * 0.5f) * BrickSize, (row + 0.5f) * BrickSize, 0f);
                    var name = FormattableString.Invariant($"brick_{row}_{column}");
                    pieces[row * Columns + column] = Brick(root, name, mesh, start, random);
                }
            }

            for (var i = 0; i < DebrisCount; i++)
            {
                pieces[Columns * Rows + i] = Debris(root, FormattableString.Invariant($"debris_{i}"), mesh, random);
            }

            VatRigidRecorder.Record(Path, root, Frames, Fps, frame =>
            {
                foreach (var piece in pieces)
                {
                    piece.Pose(frame / Fps);
                }
            });
        }

        private static Piece Brick(GameObject root, string name, Mesh mesh, Vector3 start, Random random)
        {
            var away = (start - _blastCenter).normalized;
            var velocity = away * Range(random, MinSpeed, MaxSpeed) + Vector3.up * Range(random, 0f, MaxSpeed);
            var despawn = random.NextDouble() < DespawnShare ? BrickDespawn + Range(random, 0f, DespawnSpread) : float.PositiveInfinity;
            return new Piece(Transform(root, name, mesh, BrickSize), start, velocity, Spin(random), BrickSize, 0f, despawn);
        }

        private static Piece Debris(GameObject root, string name, Mesh mesh, Random random)
        {
            var direction = new Vector3(Range(random, -1f, 1f), Range(random, 0f, 1f), Range(random, 0.2f, 1f)).normalized;
            var despawn = DebrisDespawn + Range(random, 0f, DespawnSpread);
            return new Piece(Transform(root, name, mesh, DebrisSize), _blastCenter, direction * DebrisSpeed, Spin(random), DebrisSize, BlastTime, despawn);
        }

        private static Transform Transform(GameObject root, string name, Mesh mesh, float size)
        {
            var piece = VatRigidRecorder.Piece(root.transform, name, mesh, null).transform;
            piece.localScale = Vector3.one * size;
            return piece;
        }

        private static Vector3 Spin(Random random)
        {
            var axis = new Vector3(Range(random, -1f, 1f), Range(random, -1f, 1f), Range(random, -1f, 1f)).normalized;
            return axis * Range(random, MinTurnRate, MaxTurnRate);
        }

        private static float Range(Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        private sealed class Piece
        {
            private readonly Transform _transform;
            private readonly Vector3 _start;
            private readonly Vector3 _velocity;
            private readonly Vector3 _spin;
            private readonly float _floor;
            private readonly float _spawn;
            private readonly float _despawn;
            private readonly float _landing;

            public Piece(Transform transform, Vector3 start, Vector3 velocity, Vector3 spin, float size, float spawn, float despawn)
            {
                _transform = transform;
                _start = start;
                _velocity = velocity;
                _spin = spin;
                _floor = size * 0.5f;
                _spawn = spawn;
                _despawn = despawn;
                _landing = Landing();
            }

            public void Pose(float time)
            {
                var flight = Mathf.Clamp(time - BlastTime, 0f, _landing);
                var position = _start + _velocity * flight + 0.5f * Gravity * flight * flight * Vector3.down;
                _transform.SetLocalPositionAndRotation(position, Quaternion.AngleAxis(_spin.magnitude * flight, _spin.normalized));
                _transform.gameObject.SetActive(time >= _spawn && time < _despawn);
            }

            private float Landing()
            {
                var height = _start.y - _floor;
                var up = _velocity.y;
                return (up + Mathf.Sqrt(up * up + 2f * Gravity * height)) / Gravity;
            }
        }
    }
}
