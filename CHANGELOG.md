# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
* Slack notification
* Export Calendar event

## [0.4.17] - 2021-02-10
### Changed
* Simplify navbar, use page navigation
* Only use Plausible analytics in prod environment.
### Fixed
* Bug in vote-count when adding new Game Night

## [0.4.14] - 2021-02-09
### Fixed
* Update outerHTML for unvoted count
* Use game.Id as turbo-frame identifier

## [0.4.12] - 2021-02-09
### Changed
* Removed lazy navbar element

## [0.4.11] - 2021-02-09
### Changed
* Fixed copy on game night page

## [0.4.10] - 2021-02-09
### Changed
* Removed lazy-loaded game select

## [0.4.9] - 2021-02-09
### Added
* Flatpickr for date-inputs

## [0.4.8] - 2021-02-08
### Fixed
* Navbar styling

## [0.4.7] - 2021-02-08
### Fixed
* Fix bug for checking client when sending SSE

## [0.4.6] - 2021-02-08
### Fixed
* Use correct SSE endpoint

## [0.4.5] - 2021-02-08
### Added
* Publish SSE for adding/removing votes

## [0.4.4] - 2021-02-07
### Added
* 'notification' for unvoted game nights in navbar.
### Changed
* Fetch data from table-storage in parallel

## [0.4.3] - 2021-02-04
### Added
* Styling

## [0.4.2] - 2021-02-04
### Fixed
* Bugfix in navbar

## [0.4.1] - 2021-02-04
### Added
* Add interactivity to burger button in navbar.
* Add build step for bundling js

## [0.4.0] - 2021-02-01
### Added
* Use Hotwire
* Edit Game (BGG-link, image, notes, number of players)

## [0.3.5] - 2020-12-16
### Changed
* Change due-date to tomorrow.
* Restructure dependencies

## [0.3.4] - 2020-12-03
### Added
* Game nights are now confirmed 2 days prior the earliest suggested date
### Changed
* Show Confirmed and Proposed Game nights in the game night view.

## [0.3.3] - 2020-12-02
### Changed
* Run on linux
### Added
* Functions project

## [0.2.3] - 2020-11-10
### Added
* Api endpoints

## [0.2.2] - 2020-11-07
### Added
* Plausible analytics

## [0.2.1] - 2020-11-05
### Added
* Version endpoint
* FAKE build script

## [0.2.0] - 2020-11-05
### Added
* GitHub actions (build / deploy)
* Github link in navbar

## [0.1.0] - 2020-11-05
### Added
* Create user
* Add game night
* Voting