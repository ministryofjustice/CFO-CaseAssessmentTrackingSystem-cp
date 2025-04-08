using Cfo.Cats.Application.Features.Candidates.DTOs;
using Cfo.Cats.Application.Features.Candidates.Queries.Search;

namespace Cfo.Cats.Infrastructure.Services.Candidates;

public class DummyCandidateService : ICandidateService
{
    public async Task<Result<CandidateDto>> GetByUpciAsync(string upci)
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
            RegistrationDetailsJson = "[]",
            IsActive = true
        };

        var result = Result<CandidateDto>.Success(candidate);

        return await Task.FromResult(result);
    }

    public async Task<Result<SearchResult[]>> SearchAsync(CandidateSearchQuery searchQuery)
    {
        SearchResult[] results =
        [
            new SearchResult("1CFG5437L", 1)
        ];

        var result = Result<SearchResult[]>.Success(results);

        return await Task.FromResult(result);
    }

    public Task<Result<bool>> SetStickyLocation(string upci, string location)
    {
        var result = Result<bool>.Success(false);
        return Task.FromResult(result);
    }
}
