﻿using CoAPnet;
using Microsoft.Extensions.Logging;
using System;
using Wirehome.Core.Contracts;

namespace Wirehome.Core.Hardware.CoAP
{
    public class CoapService : IService
    {
        readonly CoapFactory _coapFactory = new CoapFactory();
        readonly ILogger<CoapService> _logger;

        public CoapService(ILogger<CoapService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start()
        {

        }

        //public Task<CoapResponse> Request(CoapRequest request)
        //{
        //    if (request is null)
        //    {
        //        throw new ArgumentNullException(nameof(request));
        //    }

        //    using (var client = _coapFactory.CreateClient())
        //    {

        //    }
        //}
    }
}
