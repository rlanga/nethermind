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

using System.Net;
using Nevermind.Core;
using Nevermind.Core.Crypto;

namespace Nevermind.Discovery.RoutingTable
{
    public class NodeFactory : INodeFactory
    {
        public Node CreateNode(PublicKey id, IPEndPoint address)
        {
            var node = new Node(id)
            {
                IsDicoveryNode = false
            };
            node.InitializeAddress(address);
            return node;
        }

        public Node CreateNode(PublicKey id, string host, int port)
        {
            var node = new Node(id)
            {
                IsDicoveryNode = false
            };
            node.InitializeAddress(host, port);
            return node;
        }

        public Node CreateNode(string host, int port)
        {
            Keccak512 socketHash = Keccak512.Compute($"{host}:{port}"); 
            var node = new Node(new PublicKey(socketHash.Bytes))
            {
                IsDicoveryNode = true
            };
            node.InitializeAddress(host, port);
            return node;
        }
    }
}