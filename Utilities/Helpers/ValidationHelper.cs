using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.Helpers
{
    public class ValidationHelper
    {
        public bool IsValidRating(int rating, int minRating, int maxRating)
        {
            return rating >= minRating && rating <= maxRating;
        }
    }
}
