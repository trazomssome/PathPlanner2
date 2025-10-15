using System;
using System.Collections.ObjectModel;
using DispenserEditor.Models;

namespace DispenserEditor.ViewModels
{
    public static class SegmentTypeCollection
    {
        public static ReadOnlyCollection<PathSegmentType> All { get; } =
            Array.AsReadOnly((PathSegmentType[])Enum.GetValues(typeof(PathSegmentType)));
    }
}
