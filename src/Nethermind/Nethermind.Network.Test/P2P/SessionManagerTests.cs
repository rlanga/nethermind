﻿/*
 * Copyright (c) 2018 Demerzel Solutions Limited
 * This file is part of the Nethermind library.
 *
 * The Nethermind library is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * The Nethermind library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.
 */

using Nethermind.Core;
using Nethermind.Network.P2P;
using Nethermind.Network.Rlpx;
using NSubstitute;
using NUnit.Framework;

namespace Nethermind.Network.Test.P2P
{
    [TestFixture]
    public class SessionManagerTests
    {
        private const int ListenPort = 8002;

        [Test]
        public void Can_start_p2p_session()
        {
            SessionManager factory = new SessionManager(Substitute.For<IMessageSerializationService>(), NetTestVectors.StaticKeyA.PublicKey, ListenPort, new NullLogger());
            factory.Start(0, 5, Substitute.For<IPacketSender>(), NetTestVectors.StaticKeyB.PublicKey, 8003);
        }

        [Test]
        public void Can_start_eth_session()
        {
            SessionManager factory = new SessionManager(Substitute.For<IMessageSerializationService>(), NetTestVectors.StaticKeyA.PublicKey, ListenPort, new NullLogger());
            factory.Start(1, 62, Substitute.For<IPacketSender>(), NetTestVectors.StaticKeyB.PublicKey, 8003);
        }
    }
}