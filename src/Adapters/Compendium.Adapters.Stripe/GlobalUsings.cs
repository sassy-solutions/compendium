// -----------------------------------------------------------------------
// <copyright file="GlobalUsings.cs" company="Compendium">
//     Copyright (c) 2025 Sassy Solutions. All rights reserved.
//     Licensed under the MIT License with Attribution.
//     NO AI TRAINING: This code may NOT be used for training AI/ML models.
//     See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

global using System.Net;
global using System.Security.Cryptography;
global using System.Text;
global using System.Text.Json;
global using Compendium.Abstractions.Billing;
global using Compendium.Abstractions.Billing.Models;
global using Compendium.Core.Results;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Stripe;
global using Subscription = Compendium.Abstractions.Billing.Models.Subscription;
global using CheckoutSession = Compendium.Abstractions.Billing.Models.CheckoutSession;
global using BillingCustomer = Compendium.Abstractions.Billing.Models.BillingCustomer;
