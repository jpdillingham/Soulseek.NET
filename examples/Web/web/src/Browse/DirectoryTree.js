import React from 'react';

import { 
  List
} from 'semantic-ui-react';

const subtree = (root, onSelect) => {
  return (root || []).map(d => (
      <List className='browse-folderlist-list'>
          <List.Item>
              <List.Icon name='folder'/>
              <List.Content>
                  <List.Header onClick={(event) => onSelect(event, d)}>{d.directoryName.split('\\').pop().split('/').pop()}</List.Header>
                  <List.List>
                      {subtree(d.children, onSelect)}
                  </List.List>
              </List.Content>
          </List.Item>
      </List>
  ))
}

const DirectoryTree = ({ tree, onSelect }) => subtree(tree, onSelect);

export default DirectoryTree;