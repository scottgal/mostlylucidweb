"""Write explicit package-consumer sources without shell URL/path conversion."""
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

feed, destination = map(Path, sys.argv[1:3])
configuration = ET.Element('configuration')
sources = ET.SubElement(configuration, 'packageSources')
ET.SubElement(sources, 'clear')
ET.SubElement(sources, 'add', key='local-relevance', value=str(feed.resolve()))
ET.SubElement(sources, 'add', key='nuget.org', value='https://api.nuget.org/v3/index.json')
destination.parent.mkdir(parents=True, exist_ok=True)
ET.ElementTree(configuration).write(destination, encoding='utf-8', xml_declaration=True)
