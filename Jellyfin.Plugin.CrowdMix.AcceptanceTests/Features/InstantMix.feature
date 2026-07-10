Feature: Crowd-sourced instant mix
  As a Jellyfin listener
  I want an instant mix built from real listening-crowd similarity
  So that a mix around a seed track feels hand-picked, not random

  Scenario: Mix is built from owned tracks the crowd plays alongside the seed
    Given a library containing "Radiohead - Karma Police" and "Pixies - Where Is My Mind"
    And the crowd considers "Pixies - Where Is My Mind" similar to the seed
    When I request an instant mix seeded by "Radiohead - Karma Police"
    Then the mix contains "Pixies - Where Is My Mind"
    And the mix excludes the seed track

  Scenario: A single artist cannot dominate the mix
    Given a library with three tracks by "The Cure" all similar to the seed
    And a max of 2 tracks per artist
    When I request an instant mix seeded by "Disintegration - Plainsong"
    Then no more than 2 tracks by "The Cure" appear in the mix
