{
  description = "Description for the project";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-parts.url = "github:hercules-ci/flake-parts";
  };

  outputs =
    inputs@{ flake-parts, ... }:
    flake-parts.lib.mkFlake { inherit inputs; } {
      systems = [
        "x86_64-linux"
        "aarch64-linux"
        "x86_64-darwin"
        "aarch64-darwin"
      ];
      perSystem =
        {
          config,
          self',
          inputs',
          pkgs,
          system,
          ...
        }:
        let
          # Function to create script
          mkScript =
            name: text:
            let
              script = pkgs.writeShellScriptBin name text;
            in
            script;

          # Define your scripts/aliases
          scripts = [
            (mkScript "build" ''dotnet build src/*.sln -f net6.0 -c Release && cp _plugins/net6.0/Lidarr.Plugin.Tidal/Lidarr.Plugin.Tidal.* ~/lidarr/config/plugins/alexandru0-dev/Lidarr.Plugin.Tidal'')
          ];
        in
        {
          devShells.default = pkgs.mkShell {
            nativeBuildInputs = with pkgs; [ dotnet-sdk_8 ] ++ scripts;
          };
        };
    };
}
