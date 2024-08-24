using Mostlylucid.OpenSearch.Models;
using OpenSearch.Client;

namespace Mostlylucid.OpenSearch;

public class SearchService(OpenSearchClient client, ILogger<SearchService> logger) : BaseService
{
    
    public async Task<List<BlogIndexModel>> GetSearchResults(string language, string query, int page = 1, int pageSize = 10)
    {
        var indexName = GetBlogIndexName(language);
        var searchResponse = await client.SearchAsync<BlogIndexModel>(s => s
            .Index(indexName)
            .Source(src => src
                .Includes(i => i
                    .Fields(f => f.Title, f => f.Categories, f => f.Slug)
                )
            )
            .Skip((page-1) * pageSize)  
            .Size(pageSize) 
            .Query(q => q
                .Bool(b => b
                    .Should(
                        sh => sh.MultiMatch(mm => mm
                            .Query(query)
                            .Fields(f => f
                                    .Field(p => p.Title, 2.0)  // Boost title
                                    .Field(p => p.Categories, 2.5)  // Boost categories
                                    .Field(p => p.Content)  // No boost for content
                            )
                            .Type(TextQueryType.BestFields)
                            .Fuzziness(Fuzziness.Auto)
                        ),
                        sh => sh.Prefix(pf => pf
                            .Field(p => p.Content)
                            .Value(query)
                       
                        ),
                        sh => sh.Prefix(pf => pf
                            .Field(p => p.Title)
                            .Value(query)
                            .Boost(2.0)
                        ),
                        sh => sh.Prefix(pf => pf
                            .Field(p => p.Categories)
                            .Value(query)
                            .Boost(2.5)
                        )
                    )
                )
            )
        );
        if(!searchResponse.IsValid)
        {
            logger.LogError("Failed to search index {IndexName}: {Error}", indexName, searchResponse.DebugInformation);
            return new List<BlogIndexModel>();
        }
        return searchResponse.Documents.ToList();
    }

}