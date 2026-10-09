# Changelog

## [2.2.0](https://github.com/atc-net/atc-rest-client/compare/v2.1.0...v2.2.0) (2026-10-09)


### New features

* **builder:** add WithBody with a content type and URL-encoded form fields ([a10c01f](https://github.com/atc-net/atc-rest-client/commit/a10c01fed15ca316ba4651ca6b828e91db21875d))
* **builder:** add WithUrlEncodedForm, which also sends an empty form ([2cfc41f](https://github.com/atc-net/atc-rest-client/commit/2cfc41f276edfac09e373add4764a7f7985244a9))
* **builder:** send a Stream passed to WithBody as a binary body ([f65ad92](https://github.com/atc-net/atc-rest-client/commit/f65ad92f26dec3ae4577436b9ef8228cffcca46a))
* make the library trim- and Native AOT-ready ([89f5dc1](https://github.com/atc-net/atc-rest-client/commit/89f5dc120025d3b4de83fd7f2d5579a7a3d5c1d2))
* **options:** add composable ConfigureAtcRestClientJsonOptions ([0923ed8](https://github.com/atc-net/atc-rest-client/commit/0923ed8a0ebe812d464dc108f76dc4e2d10051ab))


### Bug fixes

* **builder:** escape query parameter keys ([15dabac](https://github.com/atc-net/atc-rest-client/commit/15dabacd1827ed9df7689d722df1007994ddcfcb))
* **builder:** format parameter values independently of the culture ([0ea5874](https://github.com/atc-net/atc-rest-client/commit/0ea5874ef65230070d4793c0ebedb1c1a962d076))
* **builder:** forward the cancellation token when reading response content ([78e43f3](https://github.com/atc-net/atc-rest-client/commit/78e43f336fd59f5104bdf96235a636e5439435ce))
* **builder:** send every repeated multipart form field ([5b1abed](https://github.com/atc-net/atc-rest-client/commit/5b1abed7905661827043b0e6411efd9cc52fe082))
* **builder:** write bool parameter values as true and false ([e31d46e](https://github.com/atc-net/atc-rest-client/commit/e31d46ee52df3cefca2d0ca0718e363c291354d5))

## [2.1.0](https://github.com/atc-net/atc-rest-client/compare/v2.0.36...v2.1.0) (2026-09-30)


### New features

* **builder:** typed error content, error metadata and filename* for binary responses ([2d3abae](https://github.com/atc-net/atc-rest-client/commit/2d3abae45d84de7270dd1f19f0585576addd7c62))


### Bug fixes

* **builder:** read error response body as string for non-text media types ([1f704fb](https://github.com/atc-net/atc-rest-client/commit/1f704fb450bf02064717dff322587de3604ac306))
