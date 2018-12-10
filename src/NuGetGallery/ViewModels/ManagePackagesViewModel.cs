﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using NuGet.Services.Entities;

namespace NuGetGallery
{
    public class ManagePackagesViewModel
    {
        public virtual User User { get; set; }

        public IEnumerable<string> Owners { get; set; }

        public dynamic ListedPackages { get; set; }

        public dynamic UnlistedPackages { get; set; }

        public OwnerRequestsViewModel OwnerRequests { get; set; }

        public ReservedNamespaceListViewModel ReservedNamespaces { get; set; }

        public bool WasMultiFactorAuthenticated { get; set; }

        public bool IsCertificatesUIEnabled { get; set; }
    }
}