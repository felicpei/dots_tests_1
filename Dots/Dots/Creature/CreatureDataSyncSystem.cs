using Dots;
using Dots;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dots
{
    [BurstCompile]
    //[RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(CreatureSystemGroup))]
    public partial struct CreatureDataSyncSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<CreatureMove> _creatureMoveLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GlobalInitialized>();
            _creatureMoveLookup = state.GetComponentLookup<CreatureMove>(true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _creatureMoveLookup.Update(ref state);

            new DataSyncJob
            {
                CreatureMoveLookup = _creatureMoveLookup,
            }.ScheduleParallel();
            state.Dependency.Complete();
        }

        [BurstCompile]
        private partial struct DataSyncJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<CreatureMove> CreatureMoveLookup;

            [BurstCompile]
            private void Execute(RefRW<CreatureProperties> creature, Entity entity, [EntityIndexInQuery] int sortKey)
            {
                if (CreatureMoveLookup.TryGetComponent(entity, out var creatureMove))
                {
                    creature.ValueRW.InMoveCopy = creatureMove.InMove;
                    creature.ValueRW.MoveSpeedCopy = creatureMove.MoveSpeedResult;
                }
            }
        }
    }
}