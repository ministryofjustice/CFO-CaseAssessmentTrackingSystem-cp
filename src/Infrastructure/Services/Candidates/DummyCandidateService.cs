using Cfo.Cats.Application.Features.Candidates.DTOs;
using Cfo.Cats.Application.Features.Candidates.Queries.Search;

namespace Cfo.Cats.Infrastructure.Services.Candidates;

public class DummyCandidateService : ICandidateService
{
    public async Task<CandidateDto?> GetByUpciAsync(string upci)
    {
        var candidate = new CandidateDto 
        { 
            Identifier = "1CFG5437L",
            FirstName = "John",
            SecondName = string.Empty,
            LastName = "Doe",
            Nationality = "British",
            Ethnicity = string.Empty,
            Primary = "NOMIS",
            Gender = "Male",
            DateOfBirth = new DateTime(2000, 01, 01),
            NomisNumber = "A6952ZA",
            EstCode = "LPI",
            RegistrationDetailsJson = "{}",
            IsActive = true
        };

        return await Task.FromResult(candidate);
    }

    public async Task<IEnumerable<SearchResult>?> SearchAsync(CandidateSearchQuery searchQuery)
    {
        List<SearchResult> results =
        [
            new SearchResult("1CFG5437L", 1)
        ];

        return await Task.FromResult(results);
    }
}
