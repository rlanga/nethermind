﻿using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethermind.BeaconNode.Configuration;
using Nethermind.BeaconNode.Containers;
using Nethermind.BeaconNode.Containers.Json;
using Nethermind.BeaconNode.Ssz;
using Nethermind.BeaconNode.Storage;
using Nethermind.BeaconNode.Tests.Helpers;
using Nethermind.Core2.Types;
using Shouldly;

namespace Nethermind.BeaconNode.Tests.Fork
{
    [TestClass]
    public class GetHeadTest
    {
        [TestMethod]
        public async Task GenesisHead()
        {
            // Arrange
            IServiceProvider testServiceProvider = TestSystem.BuildTestServiceProvider(useStore: true);
            BeaconState state = TestState.PrepareTestState(testServiceProvider);

            MiscellaneousParameters miscellaneousParameters = testServiceProvider.GetService<IOptions<MiscellaneousParameters>>().Value;
            TimeParameters timeParameters = testServiceProvider.GetService<IOptions<TimeParameters>>().Value;
            StateListLengths stateListLengths = testServiceProvider.GetService<IOptions<StateListLengths>>().Value;
            MaxOperationsPerBlock maxOperationsPerBlock = testServiceProvider.GetService<IOptions<MaxOperationsPerBlock>>().Value;

            JsonSerializerOptions options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            options.AddCortexContainerConverters();
            string debugState = System.Text.Json.JsonSerializer.Serialize(state, options);
            
            // Initialization
            ForkChoice forkChoice = testServiceProvider.GetService<ForkChoice>();
            IStore store = forkChoice.GetGenesisStore(state);

            // Act
            Hash32 headRoot = await forkChoice.GetHeadAsync(store);

            // Assert
            Hash32 stateRoot = state.HashTreeRoot(miscellaneousParameters, timeParameters, stateListLengths, maxOperationsPerBlock);
            BeaconBlock genesisBlock = new BeaconBlock(stateRoot);
            Hash32 expectedRoot = genesisBlock.SigningRoot(miscellaneousParameters, maxOperationsPerBlock);

            headRoot.ShouldBe(expectedRoot);
        }

        [TestMethod]
        public async Task ChainNoAttestations()
        {
            // Arrange
            IServiceProvider testServiceProvider = TestSystem.BuildTestServiceProvider(useStore: true);
            BeaconState state = TestState.PrepareTestState(testServiceProvider);

            MiscellaneousParameters miscellaneousParameters = testServiceProvider.GetService<IOptions<MiscellaneousParameters>>().Value;
            MaxOperationsPerBlock maxOperationsPerBlock = testServiceProvider.GetService<IOptions<MaxOperationsPerBlock>>().Value;

            // Initialization
            ForkChoice forkChoice = testServiceProvider.GetService<ForkChoice>();
            IStore store = forkChoice.GetGenesisStore(state);

            // On receiving a block of `GENESIS_SLOT + 1` slot
            BeaconBlock block1 = TestBlock.BuildEmptyBlockForNextSlot(testServiceProvider, state, signed: true);
            TestState.StateTransitionAndSignBlock(testServiceProvider, state, block1);
            AddBlockToStore(testServiceProvider, store, block1);

            // On receiving a block of next epoch
            BeaconBlock block2 = TestBlock.BuildEmptyBlockForNextSlot(testServiceProvider, state, signed: true);
            TestState.StateTransitionAndSignBlock(testServiceProvider, state, block2);
            AddBlockToStore(testServiceProvider, store, block2);

            // Act
            Hash32 headRoot = await forkChoice.GetHeadAsync(store);

            // Assert
            Hash32 expectedRoot = block2.SigningRoot(miscellaneousParameters, maxOperationsPerBlock);
            headRoot.ShouldBe(expectedRoot);
        }

        [TestMethod]
        public async Task SplitTieBreakerNoAttestations()
        {
            // Arrange
            IServiceProvider testServiceProvider = TestSystem.BuildTestServiceProvider(useStore: true);
            BeaconState state = TestState.PrepareTestState(testServiceProvider);

            MiscellaneousParameters miscellaneousParameters = testServiceProvider.GetService<IOptions<MiscellaneousParameters>>().Value;
            MaxOperationsPerBlock maxOperationsPerBlock = testServiceProvider.GetService<IOptions<MaxOperationsPerBlock>>().Value;

            // Initialization
            ForkChoice forkChoice = testServiceProvider.GetService<ForkChoice>();
            IStore store = forkChoice.GetGenesisStore(state);
            BeaconState genesisState = BeaconState.Clone(state);

            // block at slot 1
            BeaconState block1State = BeaconState.Clone(genesisState);
            BeaconBlock block1 = TestBlock.BuildEmptyBlockForNextSlot(testServiceProvider, block1State, signed: true);
            TestState.StateTransitionAndSignBlock(testServiceProvider, block1State, block1);
            AddBlockToStore(testServiceProvider, store, block1);
            Hash32 block1Root = block1.SigningRoot(miscellaneousParameters, maxOperationsPerBlock);

            // build short tree
            BeaconState block2State = BeaconState.Clone(genesisState);
            BeaconBlock block2 = TestBlock.BuildEmptyBlockForNextSlot(testServiceProvider, block2State, signed: true);
            block2.Body.SetGraffiti(new Bytes32(Enumerable.Repeat((byte)0x42, 32).ToArray()));
            TestBlock.SignBlock(testServiceProvider, block2State, block2, ValidatorIndex.None);
            TestState.StateTransitionAndSignBlock(testServiceProvider, block2State, block2);
            AddBlockToStore(testServiceProvider, store, block2);
            Hash32 block2Root = block2.SigningRoot(miscellaneousParameters, maxOperationsPerBlock);

            // Act
            Hash32 headRoot = await forkChoice.GetHeadAsync(store);

            // Assert
            Console.WriteLine("block1 {0}", block1Root);
            Console.WriteLine("block2 {0}", block2Root);
            Hash32 highestRoot = block1Root.CompareTo(block2Root) > 0 ? block1Root : block2Root;
            Console.WriteLine("highest {0}", highestRoot);
            headRoot.ShouldBe(highestRoot);
        }

        [TestMethod]
        public async Task ShorterChainButHeavierWeight()
        {
            // Arrange
            IServiceProvider testServiceProvider = TestSystem.BuildTestServiceProvider(useStore: true);
            BeaconState state = TestState.PrepareTestState(testServiceProvider);

            MiscellaneousParameters miscellaneousParameters = testServiceProvider.GetService<IOptions<MiscellaneousParameters>>().Value;
            MaxOperationsPerBlock maxOperationsPerBlock = testServiceProvider.GetService<IOptions<MaxOperationsPerBlock>>().Value;

            // Initialization
            ForkChoice forkChoice = testServiceProvider.GetService<ForkChoice>();
            IStore store = forkChoice.GetGenesisStore(state);
            BeaconState genesisState = BeaconState.Clone(state);

            // build longer tree
            Hash32 longRoot = default;
            BeaconState longState = BeaconState.Clone(genesisState);
            for (int i = 0; i < 3; i++)
            {
                BeaconBlock longBlock = TestBlock.BuildEmptyBlockForNextSlot(testServiceProvider, longState, signed: true);
                TestState.StateTransitionAndSignBlock(testServiceProvider, longState, longBlock);
                AddBlockToStore(testServiceProvider, store, longBlock);
                if (i == 2)
                {
                    longRoot = longBlock.SigningRoot(miscellaneousParameters, maxOperationsPerBlock);
                }
            }

            // build short tree
            BeaconState shortState = BeaconState.Clone(genesisState);
            BeaconBlock shortBlock = TestBlock.BuildEmptyBlockForNextSlot(testServiceProvider, shortState, signed: true);
            shortBlock.Body.SetGraffiti(new Bytes32(Enumerable.Repeat((byte)0x42, 32).ToArray()));
            TestBlock.SignBlock(testServiceProvider, shortState, shortBlock, ValidatorIndex.None);
            TestState.StateTransitionAndSignBlock(testServiceProvider, shortState, shortBlock);
            AddBlockToStore(testServiceProvider, store, shortBlock);

            Attestation shortAttestation = TestAttestation.GetValidAttestation(testServiceProvider, shortState, shortBlock.Slot, CommitteeIndex.None, signed: true);
            AddAttestationToStore(testServiceProvider, store, shortAttestation);

            // Act
            Hash32 headRoot = await forkChoice.GetHeadAsync(store);

            // Assert
            Hash32 expectedRoot = shortBlock.SigningRoot(miscellaneousParameters, maxOperationsPerBlock);
            headRoot.ShouldBe(expectedRoot);
            headRoot.ShouldNotBe(longRoot);
        }

        private void AddAttestationToStore(IServiceProvider testServiceProvider, IStore store, Attestation attestation)
        {
            TimeParameters timeParameters = testServiceProvider.GetService<IOptions<TimeParameters>>().Value;
            MiscellaneousParameters miscellaneousParameters = testServiceProvider.GetService<IOptions<MiscellaneousParameters>>().Value;
            MaxOperationsPerBlock maxOperationsPerBlock = testServiceProvider.GetService<IOptions<MaxOperationsPerBlock>>().Value;
            ForkChoice forkChoice = testServiceProvider.GetService<ForkChoice>();

            if (!store.TryGetBlock(attestation.Data.BeaconBlockRoot, out BeaconBlock? parentBlock))
            {
                throw new InvalidDataException("Cannot retrieve parent block");
            }
            
            Hash32 parentSigningRoot = parentBlock!.SigningRoot(miscellaneousParameters, maxOperationsPerBlock);
            if (!store.TryGetBlockState(parentSigningRoot, out BeaconState? preState))
            {
                throw new InvalidDataException("Cannot retrieve pre state");
            }
            
            ulong blockTime = preState!.GenesisTime + (ulong)parentBlock!.Slot * timeParameters.SecondsPerSlot;
            ulong nextEpochTime = blockTime + (ulong)timeParameters.SlotsPerEpoch * timeParameters.SecondsPerSlot;

            if (store.Time < blockTime)
            {
                forkChoice.OnTick(store, blockTime);
            }

            forkChoice.OnAttestation(store, attestation);
        }

        private void AddBlockToStore(IServiceProvider testServiceProvider, IStore store, BeaconBlock block)
        {
            TimeParameters timeParameters = testServiceProvider.GetService<IOptions<TimeParameters>>().Value;
            ForkChoice forkChoice = testServiceProvider.GetService<ForkChoice>();

            if (!store.TryGetBlockState(block.ParentRoot, out BeaconState? preState))
            {
                throw new InvalidDataException("Cannot retrieve pre state");
            }
            
            ulong blockTime = preState!.GenesisTime + (ulong)block.Slot * timeParameters.SecondsPerSlot;

            if (store.Time < blockTime)
            {
                forkChoice.OnTick(store, blockTime);
            }

            forkChoice.OnBlock(store, block);
        }
    }
}
