using FamilyTreeApiV2.Features.Import;
using FamilyTreeApiV2.Features.Persons;
using FamilyTreeApiV2.Features.Relations;
using FamilyTreeApiV2.Shared.FuzzyDates;
using FluentAssertions;

namespace FamilyTree.UnitTests.Features.Import;

public class ImportMapper_Tests
{
    [Theory]
    [InlineData("male", Gender.Male)]
    [InlineData("female", Gender.Female)]
    [InlineData("diverse", Gender.Diverse)]
    public void ToCreatePersonRequest_WhenGenderIsKnown_ShouldMapCorrectly(string v1Gender, Gender expected)
    {
        var v1 = new V1Person("id", "Anna", "Müller", null, null, v1Gender, null, null, null, null, null, null, null, null);

        var result = ImportMapper.ToCreatePersonRequest(v1);

        result.Gender.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("unknown")]
    [InlineData("")]
    public void ToCreatePersonRequest_WhenGenderIsUnknownOrNull_ShouldMapToNull(string? v1Gender)
    {
        var v1 = new V1Person("id", "Anna", "Müller", null, null, v1Gender, null, null, null, null, null, null, null, null);

        var result = ImportMapper.ToCreatePersonRequest(v1);

        result.Gender.Should().BeNull();
    }

    [Fact]
    public void ToCreatePersonRequest_ShouldMapAllTextFields()
    {
        var v1 = new V1Person("id", "Anna", "Müller", "Maria", "Schmidt", "female",
            "Berlin", null, "Hamburg", null, "Friedhof", "Dr.", "katholisch", "Notizen");

        var result = ImportMapper.ToCreatePersonRequest(v1);

        result.FirstName.Should().Be("Anna");
        result.LastName.Should().Be("Müller");
        result.MiddleNames.Should().Be("Maria");
        result.BirthName.Should().Be("Schmidt");
        result.BirthPlace.Should().Be("Berlin");
        result.DeathPlace.Should().Be("Hamburg");
        result.BurialPlace.Should().Be("Friedhof");
        result.Title.Should().Be("Dr.");
        result.Religion.Should().Be("katholisch");
        result.Notes.Should().Be("Notizen");
    }

    [Theory]
    [InlineData("biological_parent", RelationType.BiologicalParent)]
    [InlineData("adoptive_parent", RelationType.AdoptiveParent)]
    [InlineData("foster_parent", RelationType.FosterParent)]
    [InlineData("spouse", RelationType.Spouse)]
    [InlineData("partner", RelationType.Partner)]
    [InlineData("engaged", RelationType.Engaged)]
    public void ToCreateRelationRequest_ShouldMapAllRelationTypes(string v1Type, RelationType expected)
    {
        var personAId = Guid.NewGuid();
        var personBId = Guid.NewGuid();
        var v1 = new V1Relation("id", "a", "b", v1Type, null, null, null, "");

        var result = ImportMapper.ToCreateRelationRequest(v1, personAId, personBId);

        result.Type.Should().Be(expected);
        result.PersonAId.Should().Be(personAId);
        result.PersonBId.Should().Be(personBId);
    }

    [Fact]
    public void ToFuzzyDateRequest_ShouldMapPrecisionAndDate()
    {
        var v1 = new V1FuzzyDate("id", "Year", "1950-01-01T00:00:00Z", null, null, null, "ca.");

        var result = ImportMapper.ToFuzzyDateRequest(v1);

        result.Precision.Should().Be(FuzzyDatePrecision.Year);
        result.Date.Should().Be(new DateOnly(1950, 1, 1));
        result.Note.Should().Be("ca.");
        result.DatePrecision.Should().BeNull();
        result.DateTo.Should().BeNull();
    }

    [Fact]
    public void ToFuzzyDateRequest_WhenBetweenPrecision_ShouldMapDateToField()
    {
        var v1 = new V1FuzzyDate("id", "Between", "1900-01-01T00:00:00Z", null, "1910-01-01T00:00:00Z", null, null);

        var result = ImportMapper.ToFuzzyDateRequest(v1);

        result.Precision.Should().Be(FuzzyDatePrecision.Between);
        result.Date.Should().Be(new DateOnly(1900, 1, 1));
        result.DateTo.Should().Be(new DateOnly(1910, 1, 1));
    }

    [Fact]
    public void ToCreatePersonRequest_WhenBirthDateIsNull_ShouldReturnNullBirthDate()
    {
        var v1 = new V1Person("id", "Anna", "Müller", null, null, null, null, null, null, null, null, null, null, null);

        var result = ImportMapper.ToCreatePersonRequest(v1);

        result.BirthDate.Should().BeNull();
        result.DeathDate.Should().BeNull();
    }
}
