namespace GGNet;

/// <summary>Browser scrolling policy within an interactive plot area.</summary>
public enum TouchOwnership
{
	/// <summary>The plot owns touch from contact; browser scrolling and page zoom do not start inside it.</summary>
	Owned,

	/// <summary>Permit native vertical scrolling, which may cancel chart inspection even after a hold.</summary>
	ScrollY
}
