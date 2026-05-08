using MongoDB.Bson.Serialization.Attributes;

namespace MongoDbAssociate.Entities.SampleAirbnb;
[BsonIgnoreExtraElements]
public class Listing
{
    [BsonId]
    [BsonElement("_id")]
    public string Id { get; set; }

    [BsonElement("access")] public string Access { get; set; } = null!;

    [BsonElement("accommodates")]
    public int Accommodates { get; set; }

    [BsonElement("address")]
    public Address Address { get; set; } = null!;

    [BsonElement("amenities")] public List<string> Amenities { get; set; } = [];

    [BsonElement("availability")]
    public Availability Availability { get; set; } = null!;

    [BsonElement("bathrooms")]
    public decimal? Bathrooms { get; set; }

    [BsonElement("bed_type")]
    public string BedType { get; set; } = null!;

    [BsonElement("bedrooms")]
    public int Bedrooms { get; set; }

    [BsonElement("beds")]
    public int Beds { get; set; }

    [BsonElement("calendar_last_scraped")]
    public DateTime? CalendarLastScraped { get; set; }

    [BsonElement("cancellation_policy")]
    public string CancellationPolicy { get; set; } = null!;

    [BsonElement("cleaning_fee")]
    public decimal? CleaningFee { get; set; }

    [BsonElement("description")]
    public string Description { get; set; } = null!;

    [BsonElement("extra_people")]
    public decimal? ExtraPeople { get; set; }

    [BsonElement("first_review")]
    public DateTime? FirstReview { get; set; }

    [BsonElement("guests_included")]
    public decimal? GuestsIncluded { get; set; }

    [BsonElement("host")]
    public Host Host { get; set; }

    [BsonElement("house_rules")]
    public string HouseRules { get; set; }

    [BsonElement("images")]
    public Images Images { get; set; }

    [BsonElement("interaction")]
    public string Interaction { get; set; }

    [BsonElement("last_review")]
    public DateTime? LastReview { get; set; }

    [BsonElement("last_scraped")]
    public DateTime? LastScraped { get; set; }

    [BsonElement("listing_url")]
    public string ListingUrl { get; set; }

    [BsonElement("maximum_nights")]
    public string MaximumNights { get; set; }

    [BsonElement("minimum_nights")]
    public string MinimumNights { get; set; }

    [BsonElement("name")]
    public string Name { get; set; }

    [BsonElement("neighborhood_overview")]
    public string NeighborhoodOverview { get; set; }

    [BsonElement("notes")]
    public string Notes { get; set; }

    [BsonElement("number_of_reviews")]
    public int NumberOfReviews { get; set; }

    [BsonElement("price")]
    public decimal? Price { get; set; }
    
    [BsonElement("weekly_price")]
    public decimal? WeeklyPrice { get; set; }

    [BsonElement("property_type")]
    public string PropertyType { get; set; }

    [BsonElement("review_scores")]
    public ReviewScores ReviewScores { get; set; }

    [BsonElement("reviews")]
    public List<Review> Reviews { get; set; }

    [BsonElement("room_type")]
    public string RoomType { get; set; }

    [BsonElement("security_deposit")]
    public decimal? SecurityDeposit { get; set; }

    [BsonElement("space")]
    public string Space { get; set; }

    [BsonElement("summary")]
    public string Summary { get; set; }

    [BsonElement("transit")]
    public string Transit { get; set; }
}

public class Address
{
    [BsonElement("street")]
    public string Street { get; set; }

    [BsonElement("suburb")]
    public string Suburb { get; set; }

    [BsonElement("government_area")]
    public string GovernmentArea { get; set; }

    [BsonElement("market")]
    public string Market { get; set; }

    [BsonElement("country")]
    public string Country { get; set; }

    [BsonElement("country_code")]
    public string CountryCode { get; set; }

    [BsonElement("location")]
    public Location Location { get; set; }
}

public class Location
{
    [BsonElement("type")]
    public string Type { get; set; }

    [BsonElement("coordinates")]
    public List<double> Coordinates { get; set; }

    [BsonElement("is_location_exact")]
    public bool IsLocationExact { get; set; }
}

public class Availability
{
    [BsonElement("availability_30")]
    public int Availability30 { get; set; }

    [BsonElement("availability_60")]
    public int Availability60 { get; set; }

    [BsonElement("availability_90")]
    public int Availability90 { get; set; }

    [BsonElement("availability_365")]
    public int Availability365 { get; set; }
}

public class Host
{
    [BsonElement("host_id")]
    public string HostId { get; set; }

    [BsonElement("host_url")]
    public string HostUrl { get; set; }

    [BsonElement("host_name")]
    public string HostName { get; set; }

    [BsonElement("host_location")]
    public string HostLocation { get; set; }

    [BsonElement("host_about")]
    public string HostAbout { get; set; }

    [BsonElement("host_response_time")]
    public string HostResponseTime { get; set; }

    [BsonElement("host_thumbnail_url")]
    public string HostThumbnailUrl { get; set; }

    [BsonElement("host_picture_url")]
    public string HostPictureUrl { get; set; }

    [BsonElement("host_neighbourhood")]
    public string HostNeighbourhood { get; set; }

    [BsonElement("host_response_rate")]
    public int HostResponseRate { get; set; }

    [BsonElement("host_is_superhost")]
    public bool HostIsSuperhost { get; set; }

    [BsonElement("host_has_profile_pic")]
    public bool HostHasProfilePic { get; set; }

    [BsonElement("host_identity_verified")]
    public bool HostIdentityVerified { get; set; }

    [BsonElement("host_listings_count")]
    public int HostListingsCount { get; set; }

    [BsonElement("host_total_listings_count")]
    public int HostTotalListingsCount { get; set; }

    [BsonElement("host_verifications")]
    public List<string> HostVerifications { get; set; }
}

public class Images
{
    [BsonElement("thumbnail_url")]
    public string ThumbnailUrl { get; set; }

    [BsonElement("medium_url")]
    public string MediumUrl { get; set; }

    [BsonElement("picture_url")]
    public string PictureUrl { get; set; }

    [BsonElement("xl_picture_url")]
    public string XlPictureUrl { get; set; }
}

public class ReviewScores
{
    [BsonElement("review_scores_accuracy")]
    public int ReviewScoresAccuracy { get; set; }

    [BsonElement("review_scores_cleanliness")]
    public int ReviewScoresCleanliness { get; set; }

    [BsonElement("review_scores_checkin")]
    public int ReviewScoresCheckin { get; set; }

    [BsonElement("review_scores_communication")]
    public int ReviewScoresCommunication { get; set; }

    [BsonElement("review_scores_location")]
    public int ReviewScoresLocation { get; set; }

    [BsonElement("review_scores_value")]
    public int ReviewScoresValue { get; set; }

    [BsonElement("review_scores_rating")]
    public int ReviewScoresRating { get; set; }
}
[BsonIgnoreExtraElements] 
public class Review
{
    [BsonElement("_id")]
    public string Id { get; set; }

    [BsonElement("date")]
    public DateTime? Date { get; set; }

    [BsonElement("listing_id")]
    public string ListingId { get; set; }

    [BsonElement("reviewer_id")]
    public string ReviewerId { get; set; }

    [BsonElement("reviewer_name")]
    public string ReviewerName { get; set; }

    [BsonElement("comments")]
    public string Comments { get; set; }
}