# Build OctoGhast project

require 'rake'
require 'date'

# Include any sub rake files
Dir.glob('**/*.Rakefile').each { |r| import r}

desc "Compile OctoGhast"
task :compile do
	sh "mkdir output"
	# Build OctoGhast.Spatial:
	sh "dmcs -out:output/OctoGhast.Spatial.dll \
		-target:library OctoGhast.Spatial/**/*.cs"

	sh "dmcs -out:output/OctoGhast.DataStructures.dll \
		-r:output/OctoGhast.Spatial.dll \
		-target:library OctoGhast.DataStructures/**/*.cs"

	sh "dmcs -out:output/OctoGhast.MapGeneration.dll \
		-r:output/OctoGhast.Spatial.dll \
		-r:output/OctoGhast.DataStructures.dll \
		-target:library OctoGhast.MapGeneration/**/*.cs"

	sh "dmcs -out:output/OctoGhast.dll \
		-r:OctoGhast.MapGeneration.dll \
		-r:output/OctoGhast.DataStructure.dll \
		-r:lib/libtcod/libtcod-net.dll \
		-r:output/OctoGhast.Spatial.dll \
		-target:library OctoGhast/**/*.cs"	
end