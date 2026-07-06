namespace GGNet.Shapes;

// The closed set of data-space shapes a geom can emit. Being a union, the
// composer's dispatch is exhaustiveness-checked: adding a shape here is a
// compile error at every switch until it renders — a shape can no longer be
// silently dropped.
internal union Shape(ABLine, Area, Circle, HLine, Line, MultiPolygon, Path, Polygon, Rectangle, Text, VLine);
