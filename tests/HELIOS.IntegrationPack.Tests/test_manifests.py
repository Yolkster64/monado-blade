import json
import os
import pathlib
import unittest

ROOT = pathlib.Path(os.environ.get("HELIOS_PACK_ROOT", pathlib.Path(__file__).resolve().parents[2])).resolve()


class IntegrationPackManifestTests(unittest.TestCase):
    def load(self, relative: str):
        path = ROOT / relative
        self.assertTrue(path.exists(), f"missing manifest: {relative}")
        with path.open(encoding="utf-8-sig") as handle:
            return json.load(handle)

    def test_all_json_files_parse(self):
        for path in ROOT.glob("config/**/*.json"):
            with self.subTest(path=path.relative_to(ROOT)):
                with path.open(encoding="utf-8-sig") as handle:
                    json.load(handle)

    def test_bundle_references_exist_in_catalog(self):
        catalog = self.load("config/software/software-catalog.integration.json")
        bundles = self.load("config/software/software-bundles.core-common-cross.json")
        package_ids = {item["id"] for item in catalog["packages"]}
        for bundle in bundles["bundles"]:
            with self.subTest(bundle=bundle["id"]):
                self.assertEqual([], sorted(set(bundle["packages"]) - package_ids))

    def test_security_baseline_keeps_confirmation_gates(self):
        baseline = self.load("config/security/security-baseline.manifest.json")
        self.assertTrue(baseline["dangerousActionsRequireTypedConfirmation"])
        self.assertTrue(baseline["scriptsMustMatchManifestHash"])
        self.assertTrue(baseline["failClosedOnUnknownScanStatus"])

    def test_installer_defaults_to_dry_run(self):
        script = (ROOT / "scripts/windows/Install-HeliosBundle.ps1").read_text(encoding="utf-8-sig")
        self.assertIn("[switch]$Execute", script)
        self.assertIn("DRY RUN", script)


if __name__ == "__main__":
    unittest.main()
