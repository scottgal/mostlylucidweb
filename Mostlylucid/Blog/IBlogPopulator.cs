﻿using Mostlylucid.Models.Blog;

namespace Mostlylucid.Blog;

public interface IBlogPopulator
{
    Task Populate(CancellationToken cancellationToken);


}