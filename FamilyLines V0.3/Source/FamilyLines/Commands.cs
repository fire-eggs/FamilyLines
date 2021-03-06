﻿using System.Windows;

namespace KBS.FamilyLines
{
    /// <summary>
    /// Routed commands container class. 
    /// </summary>
    public static class Commands
    {
        /// <summary>
        /// Add a child to the current person.
        /// </summary>
        public static readonly RoutedEvent AddChild;

        /// <summary>
        /// Add a parent to the current person.
        /// </summary>
        public static readonly RoutedEvent AddParent;

        /// <summary>
        /// Add a spouse to the current person.
        /// </summary>
        public static readonly RoutedEvent AddSpouse;

        /// <summary>
        /// View/edit spouse relationship details for the current person.
        /// </summary>
        public static readonly RoutedEvent EditMarriage;

        /// <summary>
        /// View a different spouse.
        /// </summary>
        public static readonly RoutedEvent ViewSpouse;

        static Commands()
        {
            AddChild = EventManager.RegisterRoutedEvent("AddChild", RoutingStrategy.Bubble,
                                                        typeof(RoutedEventHandler), typeof(Commands));
            AddParent = EventManager.RegisterRoutedEvent("AddParent", RoutingStrategy.Bubble,
                                                        typeof(RoutedEventHandler), typeof(Commands));
            AddSpouse = EventManager.RegisterRoutedEvent("AddSpouse", RoutingStrategy.Bubble,
                                                         typeof (RoutedEventHandler), typeof (Commands));
            EditMarriage = EventManager.RegisterRoutedEvent("EditMarriage", RoutingStrategy.Bubble,
                                                        typeof(RoutedEventHandler), typeof(Commands));
            ViewSpouse = EventManager.RegisterRoutedEvent("ViewSpouse", RoutingStrategy.Bubble,
                                                        typeof(RoutedEventHandler), typeof(Commands));
        }
    }
}
