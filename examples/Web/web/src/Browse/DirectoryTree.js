import React from 'react';

import { 
  List
} from 'semantic-ui-react';

const subtree = (root, selectedDirectoryName, onSelect) => {
  return (root || []).map((d, index) => {
      const selected = d.directoryName === selectedDirectoryName;
      return (
        <List key={index} className='browse-folderlist-list'>
          <List.Item>
              <List.Icon 
                name={selected ? 'folder open' : 'folder'} 
              />
              <List.Content>
                  <List.Header className='browse-folderlist-header' onClick={(event) => onSelect(event, d)}>{d.directoryName.split('\\').pop().split('/').pop()}</List.Header>
                  <List.List>
                      {subtree(d.children, selectedDirectoryName, onSelect)}
                  </List.List>
              </List.Content>
          </List.Item>
      </List>)
    })
}

const DirectoryTree = ({ tree, selectedDirectoryName, onSelect }) => subtree(tree, selectedDirectoryName, onSelect);

export default DirectoryTree;