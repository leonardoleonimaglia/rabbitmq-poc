﻿using System;
using System.Net.Sockets;
using RabbitMq.Poc.Infra.CC.EventBus.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace RabbitMq.Poc.Infra.CC.EventBus
{
    public class EventBusPersistentConnection : IEventBusPersistentConnection, IDisposable
    {
        private readonly IConnectionFactory _connectionFactory;
        private IConnection _connection;
        private bool _disposed;

        private readonly object _syncLock = new object();

        public EventBusPersistentConnection(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public bool IsConnected =>
            _connection != null && _connection.IsOpen && !_disposed;

        public bool TryConnect()
        {
            lock (_syncLock)
            {
                _connection = _connectionFactory.CreateConnection();

                if (!IsConnected)
                    return false;

                _connection.ConnectionShutdown += OnConnectionShutdown;
                _connection.CallbackException += OnCallbackException;
                _connection.ConnectionBlocked += OnConnectionBlocked;

                return true;
            }
        }

        public IModel CreateModel()
        {
            if (!IsConnected)
                throw new InvalidOperationException("No connections are available to create the bus channel.");

            return _connection.CreateModel();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _connection.Dispose();
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed)
                return;

            TryConnect();
        }

        private void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed)
                return;

            TryConnect();
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (_disposed)
                return;

            TryConnect();
        }
    }
}