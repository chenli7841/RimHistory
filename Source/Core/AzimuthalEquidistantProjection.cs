using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimHistory.Core
{
    public class AzimuthalEquidistantProjection
    {
        public const double EarthRadiusKm = 6371.0088d;
        private const double DegreesToRadians = Math.PI / 180d;
        private const double RadiansToDegrees = 180d / Math.PI;
        private readonly double centerLatitude;
        private readonly double centerLongitude;
        private readonly double sinCenterLatitude;
        private readonly double cosCenterLatitude;

        public AzimuthalEquidistantProjection() : this(34.5, 111.5) { }

        public AzimuthalEquidistantProjection(double centerLatitudeDegrees, double centerLongitudeDegrees)
        {
            if (double.IsNaN(centerLatitudeDegrees) || double.IsInfinity(centerLatitudeDegrees)
                || centerLatitudeDegrees < -90d || centerLatitudeDegrees > 90d)
                throw new ArgumentOutOfRangeException(nameof(centerLatitudeDegrees));
            if (double.IsNaN(centerLongitudeDegrees) || double.IsInfinity(centerLongitudeDegrees)
                || centerLongitudeDegrees < -180d || centerLongitudeDegrees > 180d)
                throw new ArgumentOutOfRangeException(nameof(centerLongitudeDegrees));

            centerLatitude = centerLatitudeDegrees * DegreesToRadians;
            centerLongitude = centerLongitudeDegrees * DegreesToRadians;
            sinCenterLatitude = Math.Sin(centerLatitude);
            cosCenterLatitude = Math.Cos(centerLatitude);
        }

        public (double degreeX, double degreeY) Project(double historicalLatitude, double historicalLongitude)
        {
            double latitude = historicalLatitude * DegreesToRadians;
            double deltaLongitude = NormalizeRadians((historicalLongitude * DegreesToRadians) - centerLongitude);
            double sinLatitude = Math.Sin(latitude);
            double cosLatitude = Math.Cos(latitude);
            double cosCentralAngle = Clamp(sinCenterLatitude * sinLatitude
                + cosCenterLatitude * cosLatitude * Math.Cos(deltaLongitude), -1d, 1d);
            double centralAngle = Math.Acos(cosCentralAngle);
            if (centralAngle < 1e-12d) return (0d, 0d);
            if (Math.PI - centralAngle < 1e-9d)
                throw new ArgumentOutOfRangeException("coordinate", "Azimuthal equidistant projection is undefined at the antipode.");

            double scale = centralAngle / Math.Sin(centralAngle);
            double x = EarthRadiusKm * scale * cosLatitude * Math.Sin(deltaLongitude);
            double y = EarthRadiusKm * scale * (cosCenterLatitude * sinLatitude
                - sinCenterLatitude * cosLatitude * Math.Cos(deltaLongitude));
            return (x, y);
        }

        public (double historicalLatitude, double historicalLongitude) Unproject(double projectedX, double projectedY)
        {
            double x = projectedX / EarthRadiusKm;
            double y = projectedY / EarthRadiusKm;
            double centralAngle = Math.Sqrt(x * x + y * y);
            if (centralAngle < 1e-12d) return (centerLatitude * RadiansToDegrees, centerLongitude * RadiansToDegrees);
            if (centralAngle >= Math.PI - 1e-9d)
                throw new ArgumentOutOfRangeException("coordinate", "Unprojected point is at or beyond the projection antipode.");

            double sinCentralAngle = Math.Sin(centralAngle);
            double cosCentralAngle = Math.Cos(centralAngle);
            double latitude = Math.Asin(Clamp(cosCentralAngle * sinCenterLatitude
                + y * sinCentralAngle * cosCenterLatitude / centralAngle, -1d, 1d));
            double longitude = centerLongitude + Math.Atan2(
                x * sinCentralAngle,
                centralAngle * cosCenterLatitude * cosCentralAngle
                    - y * sinCenterLatitude * sinCentralAngle);
            return (latitude * RadiansToDegrees, NormalizeDegrees(longitude * RadiansToDegrees));
        }

        public (double historicalLatitude, double historicalLongitude) ToHistoricalCoordinate(
            double gameLatitude, double gameLongitude, double scaleFactor)
        {
            if (double.IsNaN(scaleFactor) || double.IsInfinity(scaleFactor) || scaleFactor <= 0d)
                throw new ArgumentOutOfRangeException(nameof(scaleFactor));
            var projected = Project(gameLatitude, gameLongitude);
            return Unproject(projected.degreeX / scaleFactor, projected.degreeY / scaleFactor);
        }

        private static double NormalizeRadians(double value)
        {
            while (value > Math.PI) value -= 2d * Math.PI;
            while (value < -Math.PI) value += 2d * Math.PI;
            return value;
        }

        private static double NormalizeDegrees(double value)
        {
            while (value > 180d) value -= 360d;
            while (value < -180d) value += 360d;
            return value;
        }

        private static double Clamp(double value, double minimum, double maximum) =>
            Math.Max(minimum, Math.Min(maximum, value));
    }
}
