using Deploys;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Dots
{
    public static class PhysicsHelper
    {
        public static uint GetLayer(ETeamId teamId, EHitRule hitSameTeam)
        {
            switch (teamId)
            {
                case ETeamId.Player:
                    return hitSameTeam > 0 ? PhysicsLayers.Creature : PhysicsLayers.Monster;
                case ETeamId.Monster:
                    return hitSameTeam > 0 ? PhysicsLayers.Creature : PhysicsLayers.Character;
                default:
                    return PhysicsLayers.Creature;
            }
        }

        public static bool Overlap(CollisionWorld world, uint layer, float3 pos, float radius, NativeList<DistanceHit> result)
        {
            return world.OverlapSphere(pos, radius, ref result, PhysicsLayers.GetFilter(layer));
        }

        public static bool RayCast(CollisionWorld world, uint layer, float3 pos, float3 direction, float castDist, out RaycastHit hit)
        {
            var filter = PhysicsLayers.GetFilter(layer);
            var ray = new RaycastInput
            {
                Start = pos,
                End = pos + direction * castDist,
                Filter = filter,
            };

            return world.CastRay(ray, out hit);
        }

        public static bool RayCastAll(CollisionWorld world, uint layer, float3 pos, float3 direction, float castDist, NativeList<RaycastHit> allHits)
        {
            var filter = PhysicsLayers.GetFilter(layer);
            var ray = new RaycastInput
            {
                Start = pos,
                End = pos + direction * castDist,
                Filter = filter,
            };
            return world.CastRay(ray, ref allHits);
        }


        public static bool GetNearestEnemy(Entity attacker, CollisionWorld world, float3 pos, float radius, ComponentLookup<CreatureTag> creatureLookup, ComponentLookup<InDeadTag> deadLookup, ComponentLookup<DisableAutoTargetTag> disableAutoTarget,
            ETeamId teamId, out Entity result, EHitRule hitRule = EHitRule.Enemy)
        {
            result = Entity.Null;

            var enemyLayer = GetLayer(teamId, hitRule);

            //overlap
            var allHits = new NativeList<DistanceHit>(Allocator.Temp);
            var minDist = float.MaxValue;
            var bFind = false;

            if (world.OverlapSphere(pos, radius, ref allHits, PhysicsLayers.GetFilter(enemyLayer)))
            {
                for (var i = 0; i < allHits.Length; i++)
                {
                    var hitEntity = allHits[i].Entity;

                    if (creatureLookup.TryGetComponent(hitEntity, out var reader))
                    {
                        if (deadLookup.IsComponentEnabled(hitEntity))
                        {
                            continue;
                        }

                        if (disableAutoTarget.IsComponentEnabled(hitEntity))
                        {
                            continue;
                        }

                        if (hitRule == EHitRule.EnemyAndTeam)
                        {
                        }

                        //未死亡并且是不同的team才产生伤害
                        if (DamageHelper.CheckHitRule(hitRule, attacker, teamId, hitEntity, reader.TeamId))
                        {
                            var distSq = math.distancesq(pos, allHits[i].Position);
                            if (distSq < minDist)
                            {
                                minDist = distSq;
                                bFind = true;
                                result = hitEntity;
                            }
                        }
                    }
                }
            }

            allHits.Dispose();
            return bFind;
        }

        public static bool GetFarEnemy(Entity attacker, CollisionWorld world, float3 pos, float radius, ComponentLookup<CreatureTag> creatureLookup, ComponentLookup<InDeadTag> deadLookup, ComponentLookup<DisableAutoTargetTag> disableAutoTarget,
            ETeamId teamId, out Entity result, EHitRule hitRule = EHitRule.Enemy)
        {
            result = Entity.Null;

            var enemyLayer = GetLayer(teamId, hitRule);

            //overlap
            var allHits = new NativeList<DistanceHit>(Allocator.Temp);
            var maxDist = float.MinValue;
            var bFind = false;

            if (world.OverlapSphere(pos, radius, ref allHits, PhysicsLayers.GetFilter(enemyLayer)))
            {
                for (var i = 0; i < allHits.Length; i++)
                {
                    var hitEntity = allHits[i].Entity;

                    if (creatureLookup.TryGetComponent(hitEntity, out var reader))
                    {
                        if (deadLookup.IsComponentEnabled(hitEntity))
                        {
                            continue;
                        }

                        if (disableAutoTarget.IsComponentEnabled(hitEntity))
                        {
                            continue;
                        }

                        //未死亡并且是不同的team才产生伤害
                        if (DamageHelper.CheckHitRule(hitRule, attacker, teamId, hitEntity, reader.TeamId))
                        {
                            var distSq = math.distancesq(pos, allHits[i].Position);
                            if (distSq > maxDist)
                            {
                                maxDist = distSq;
                                bFind = true;
                                result = hitEntity;
                            }
                        }
                    }
                }
            }

            allHits.Dispose();
            return bFind;
        }

        public static NativeList<Entity> OverlapEnemies(Entity attacker, CollisionWorld world, float3 pos, float radius, 
            ComponentLookup<CreatureTag> creatureLookup, ComponentLookup<InDeadTag> deadLookup, ETeamId teamId,
            EHitRule hitRule = EHitRule.Enemy)
        {
            var enemyLayer = GetLayer(teamId, hitRule);
            var result = new NativeList<Entity>(Allocator.Temp);

            //overlap
            var allHits = new NativeList<DistanceHit>(Allocator.Temp);
            if (world.OverlapSphere(pos, radius, ref allHits, PhysicsLayers.GetFilter(enemyLayer)))
            {
                for (var i = 0; i < allHits.Length; i++)
                {
                    var hitEntity = allHits[i].Entity;

                    if (creatureLookup.TryGetComponent(hitEntity, out var reader))
                    {
                        if (deadLookup.IsComponentEnabled(hitEntity))
                        {
                            continue;
                        }

                        if (DamageHelper.CheckHitRule(hitRule, attacker, teamId, hitEntity, reader.TeamId))
                        {
                            result.Add(hitEntity);
                        }
                    }
                }
            }

            allHits.Dispose();
            return result;
        }

        public static NativeList<Entity> RayCastEnemy(Entity attacker, CollisionWorld world, float3 pos, float3 direction, float castDist, ComponentLookup<CreatureTag> creatureLookup,
            ComponentLookup<InDeadTag> deadLookup, ETeamId teamId, EHitRule hitRule)
        {
            var enemyLayer = GetLayer(teamId, hitRule);
            var result = new NativeList<Entity>(Allocator.Temp);
            var allHits = new NativeList<RaycastHit>(Allocator.Temp);
            var filter = PhysicsLayers.GetFilter(enemyLayer);
            var ray = new RaycastInput
            {
                Start = pos,
                End = pos + direction * castDist,
                Filter = filter,
            };

            if (world.CastRay(ray, ref allHits))
            {
                for (var i = 0; i < allHits.Length; i++)
                {
                    var hitEntity = allHits[i].Entity;

                    if (creatureLookup.TryGetComponent(hitEntity, out var reader))
                    {
                        if (deadLookup.IsComponentEnabled(hitEntity))
                        {
                            continue;
                        }

                        if (DamageHelper.CheckHitRule(hitRule, attacker, teamId, hitEntity, reader.TeamId))
                        {
                            result.Add(hitEntity);
                        }
                    }
                }
            }

            allHits.Dispose();
            return result;
        }

        public static NativeList<Entity> SphereCastAllEnemies(Entity attacker, CollisionWorld world, float3 pos, float3 direction, float castDist, float radius,
            ComponentLookup<CreatureTag> creatureLookup, ComponentLookup<InDeadTag> deadLookup, ETeamId teamId, EHitRule hitRule)
        {
            var enemyLayer = GetLayer(teamId, hitRule);

            var result = new NativeList<Entity>(Allocator.Temp);
            var allHits = new NativeList<ColliderCastHit>(Allocator.Temp);
            var filter = PhysicsLayers.GetFilter(enemyLayer);

            if (castDist > 0.001f && castDist < 100f)
            {
                if (world.SphereCastAll(pos, radius, direction, castDist, ref allHits, filter))
                {
                    for (var i = 0; i < allHits.Length; i++)
                    {
                        var hitEntity = allHits[i].Entity;


                        if (creatureLookup.TryGetComponent(hitEntity, out var reader))
                        {
                            if (deadLookup.IsComponentEnabled(hitEntity))
                            {
                                continue;
                            }

                            if (DamageHelper.CheckHitRule(hitRule, attacker, teamId, hitEntity, reader.TeamId))
                            {
                                result.Add(hitEntity);
                            }
                        }
                    }
                }
            }

            allHits.Dispose();
            return result;
        }


        public static float3 Move(CollisionWorld world, float3 pos, float3 dir, float dist, float radius)
        {
            if (RayCast(world, PhysicsLayers.DontMove, pos, dir, dist + radius, out var hit))
            {
                var angle = MathHelper.AngleSigned(dir, -hit.SurfaceNormal, new float3(0, 0, -1));
                if (math.abs(angle) > 5)
                {
                    angle = angle > 0 ? 90 - angle : -90 - angle;
                    var newDir = MathHelper.RotateForward(dir, angle);
                    if (!RayCast(world, PhysicsLayers.DontMove, pos, newDir, dist + radius, out _))
                    {
                        return pos + newDir * dist;
                    }
                }

                //反向查找下, 如果也不能移动，则自由不限制了, 防止卡死
                if (RayCast(world, PhysicsLayers.DontMove, pos, -dir, dist + radius, out _))
                {
                    return pos + dir * dist;
                }

                return pos;
            }

            return pos + dir * dist;
        }


        public static float3 GetGroundPos(float3 sourcePos, CollisionWorld world)
        {
            while (true)
            {
                //出生点设置为地板
                var startPos = sourcePos + math.up() * 30f;
                var filter = new CollisionFilter
                {
                    BelongsTo = ~0u, // all 1s, so all layers, collide with everything, 
                    CollidesWith = PhysicsLayers.DontMove,
                    GroupIndex = 0
                };
                if (world.SphereCast(startPos, 1f, -math.up(), 100, out var hitObstacle, filter))
                {
                    sourcePos = hitObstacle.Position;
                }

                return sourcePos;
            }
        }
    }
}