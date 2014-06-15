# Build OctoGhast project

require 'rake'
require 'date'

# Include any sub rake files
Dir.glob('**/*.Rakefile').each { |r| import r}

desc "Create output directory"
directory "output"

desc "Compile OctoGhast"
task :compile => [:output] do
        # Build OctoGhast.Spatial:
        sh "dmcs -out:output/OctoGhast.Spatial.dll -target:library -recurse:OctoGhast.Spatial/*.cs"
        # Build OctoGhast.DataStructures:
        sh "dmcs -out:output/OctoGhast.DataStructures.dll -r:lib/libtcod/libtcod-net.dll -r:output/OctoGhast.Spatial.dll -target:library -recurse:OctoGhast.DataStructures/*.cs"
        # Build OctoGhast.MapGeneration:
        sh "dmcs -out:output/OctoGhast.MapGeneration.dll -r:System.Data -r:lib/libtcod/libtcod-net.dll -r:output/OctoGhast.Spatial.dll -r:output/OctoGhast.DataStructures.dll -target:library -recurse:OctoGhast.MapGeneration/*.cs"
        # Build OctoGhast
        sh "dmcs -out:output/OctoGhast.exe -r:output/OctoGhast.MapGeneration.dll -r:output/OctoGhast.DataStructures.dll -r:lib/libtcod/libtcod-net.dll -r:output/OctoGhast.Spatial.dll -target:exe -recurse:OctoGhast/*.cs"
end
