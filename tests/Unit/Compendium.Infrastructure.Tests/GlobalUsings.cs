// -----------------------------------------------------------------------
// <copyright file="GlobalUsings.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

global using System;
global using System.Collections.Concurrent;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using Compendium.Abstractions.EventSourcing;
global using Compendium.Core.Domain.Events;
global using Compendium.Core.Results;
global using Compendium.Infrastructure.EventSourcing;
global using Compendium.Infrastructure.Observability;
global using Compendium.Multitenancy;
global using FluentAssertions;
global using Microsoft.Extensions.Logging;
global using NSubstitute;
global using Xunit;
global using Xunit.Abstractions;
