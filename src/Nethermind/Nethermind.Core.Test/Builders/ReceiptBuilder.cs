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

using Nethermind.Core.Crypto;
using Nethermind.Dirichlet.Numerics;

namespace Nethermind.Core.Test.Builders
{
    public class ReceiptBuilder : BuilderBase<TransactionReceipt>
    {
        public ReceiptBuilder()
        {
            TestObjectInternal = new TransactionReceipt();
            TestObjectInternal.Logs = new [] {new LogEntry(Address.Zero, new byte[0], new [] {Keccak.Zero}),};
        }

        public ReceiptBuilder WithState(Keccak state)
        {
            TestObjectInternal.PostTransactionState = state;
            return this;
        }

        public ReceiptBuilder WithLogs(LogEntry[] logs)
        {
            TestObjectInternal.Logs = logs;
            return this;
        }

        //TODO: Transaction hash for transaction receipt builder
        public ReceiptBuilder WithTransactionHash(Keccak hash)
        {
            TestObject.TransactionHash = hash;
            return this;
        }

        public ReceiptBuilder WithBlockNumber(UInt256 number)
        {
            TestObject.BlockNumber = number;
            return this;
        }

        public ReceiptBuilder WithBlockHash(Keccak hash)
        {
            TestObject.BlockHash = hash;
            return this;
        }

        public ReceiptBuilder WithTransactionIndex(int index)
        {
            TestObjectInternal.Index = index;
            return this;
        }
    }
}